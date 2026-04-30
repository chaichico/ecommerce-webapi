# Fix Docker Delay: SQL Server Ready ช้ากว่า Web API

## อาการ
- รัน `docker compose up -d --build` แล้ว container ของ db และ webapi ขึ้นทั้งคู่
- แต่ Web API ล้มเหลวช่วงเริ่มต้น เพราะเชื่อมต่อฐานข้อมูลไม่ทัน
- สาเหตุหลักคือ db container อยู่สถานะ running แล้ว แต่ SQL Server ภายในยังไม่พร้อมรับ query

## Root Cause
- `depends_on` แบบปกติควบคุมแค่ลำดับ start ไม่ได้รอ readiness จริง
- แอปมีการแตะฐานข้อมูลตั้งแต่ startup (เช่น seeding) ทำให้เจอ race condition
- ไม่มี healthcheck ของ db และไม่มี retry policy ตอนเชื่อมต่อจาก EF Core

## วิธีแก้ (แนะนำทำครบทั้ง 3 จุด)
1. เพิ่ม healthcheck ให้ service db ใน `docker-compose.yml`
2. เปลี่ยน `depends_on` ของ webapi ให้รอจน db healthy
3. เปิด `EnableRetryOnFailure` ใน `Program.cs` สำหรับ SQL Server provider

## ตัวอย่าง docker-compose.yml (เฉพาะส่วนสำคัญ)

```yaml
services:
  webapi:
    build: .
    container_name: ecommerce-api
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__DefaultConnection=Server=db,1433;Database=EcommerceDb;User=sa;Password=YourStrong!Pass123;TrustServerCertificate=True;
      - ADMIN_USERNAME=admin
      - ADMIN_PASSWORD=Admin@123
      - JWT_SECRET=YourSuperSecretKeyForJWT_MinimumLength32Characters!
      - JWT_ISSUER=EcommerceAPI
      - JWT_AUDIENCE=EcommerceClient
      - JWT_EXPIRY_MINUTES=60
      - ENCRYPTION_KEY=YourEncryptionKey32CharactersLong!
    depends_on:
      db:
        condition: service_healthy

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver
    environment:
      - SA_PASSWORD=YourStrong!Pass123
      - ACCEPT_EULA=Y
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $$SA_PASSWORD -Q \"SELECT 1\" -C || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 20
      start_period: 20s

volumes:
  sqlserver_data:
```

หมายเหตุ:
- ใช้ `$$SA_PASSWORD` เพื่อ escape เครื่องหมาย `$` ใน compose
- ถ้า image ไม่มี `sqlcmd` ที่ path นี้ ให้ปรับ path ตาม image ที่ใช้งานจริง

## ตัวอย่าง Program.cs (EF Core Retry)
เพิ่ม retry policy ตอน `AddDbContext`

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));
```

## คำสั่งรัน
- `docker compose up -d --build`
- ถ้า compose version รองรับ สามารถใช้ `docker compose up -d --build --wait`

## วิธีตรวจสอบหลังแก้
- `docker compose ps` แล้วดูว่า service db เป็น healthy
- `docker compose logs db` เพื่อเช็ก SQL Server พร้อมรับ connection
- `docker compose logs webapi` เพื่อยืนยันว่าไม่มี exception ตอน startup/seeding
- เรียก endpoint ของ API เพื่อยืนยันว่าเชื่อม DB ได้จริง

## สรุป
การแก้ที่ถูกต้องคือเปลี่ยนจากรอแค่ container start ไปเป็นรอ database readiness พร้อมเสริม retry ที่ชั้นแอป เพื่อลดโอกาสเจอ race condition ตอนเริ่มระบบ
