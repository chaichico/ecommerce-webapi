# Docker Compose: Migration Job แยก Service (แนะนำ)

อัปเดตล่าสุด: 6 พฤษภาคม 2026

## เป้าหมาย

ต้องการให้คำสั่งเดียว:

```bash
docker compose up -d --build
```

ทำงานตามลำดับนี้โดยอัตโนมัติ:

1. `db` พร้อมใช้งาน (healthy)
2. `migrate` รัน EF Core migration แล้วจบ
3. `webapi` เริ่มทำงานหลัง migration สำเร็จเท่านั้น

หลักคิดนี้ช่วยแยกหน้าที่ชัดเจน:

- `db` = เก็บข้อมูล
- `migrate` = schema change แบบ one-shot
- `webapi` = business runtime

---

## ทำไมแนวทางนี้เป็นมาตรฐานที่ดี

- ลด race condition เมื่อมีหลาย API instance
- เห็นความผิดพลาดเร็ว (fail fast) ถ้า migration fail
- รองรับการ scale API ในอนาคต โดยไม่ให้ทุก instance แย่งกัน migrate
- แยก concern ชัดเจน ทำให้ debug และ audit ง่ายขึ้น

---

## สถาปัตยกรรมที่แนะนำ

```text
+---------+      healthy      +-------------+      success      +---------+
|   db    | --------------->  |   migrate   | --------------->  | webapi  |
+---------+                   +-------------+                    +---------+
```

เงื่อนไขสำคัญ:

- `db` ต้องมี `healthcheck`
- `migrate` ต้อง `depends_on: db: condition: service_healthy`
- `webapi` ต้อง `depends_on: migrate: condition: service_completed_successfully`

---

## ตัวอย่าง docker-compose.yml (Pattern หลัก)

> ตัวอย่างนี้เป็น pattern อ้างอิง ให้ปรับค่าตามโปรเจกต์จริง

```yaml
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: sqlserver
    env_file:
      - .env
    environment:
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

  migrate:
    build:
      context: .
      dockerfile: Dockerfile
      target: build
    container_name: ecommerce-migrate
    env_file:
      - .env
    depends_on:
      db:
        condition: service_healthy
    restart: "no"
    command: ["dotnet", "ef", "database", "update", "--project", "ecommerce.csproj"]

  webapi:
    build: .
    container_name: ecommerce-api
    ports:
      - "8080:8080"
    env_file:
      - .env
    environment:
      - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Production}
      - ASPNETCORE_URLS=http://+:8080
    depends_on:
      db:
        condition: service_healthy
      migrate:
        condition: service_completed_successfully

volumes:
  sqlserver_data:
```

---

## แนวทางประสิทธิภาพ (แนะนำเมื่อเริ่มนิ่ง)

ตัวอย่างด้านบนใช้งานง่าย แต่ service `migrate` ใช้ SDK image ซึ่งค่อนข้างใหญ่

เมื่อระบบเริ่มนิ่ง แนะนำเปลี่ยนไปใช้ **EF Migration Bundle** เพื่อลดเวลาเริ่มและลดขนาด image:

1. build stage สร้าง bundle (`dotnet ef migrations bundle`)
2. สร้าง image เบาเฉพาะไฟล์ bundle
3. ให้ `migrate` service รัน bundle แล้ว exit

ข้อดี:

- startup เร็วขึ้น
- image เล็กลง
- พฤติกรรม migration คงที่มากขึ้นใน CI/CD และ production

---

## Fail Fast ที่ควรมี

- ตั้ง `restart: "no"` ให้ `migrate` เพื่อไม่ให้ loop
- ให้ `webapi` ขึ้นกับ `service_completed_successfully` ของ `migrate`
- ถ้า migration fail ให้ stack แสดง error ชัดเจนทันที
- ใน runtime app (`Program.cs`) ไม่ควรเรียก `Database.Migrate()` ซ้ำ ถ้าใช้ pattern นี้แล้ว

---

## วิธีใช้งานประจำวัน

### รันทั้งหมด

```bash
docker compose up -d --build
```

### ดูสถานะ migration

```bash
docker compose ps
docker compose logs migrate
```

### กรณีแก้ model แล้วเพิ่ม migration ใหม่

```bash
dotnet ef migrations add <MigrationName>
docker compose up -d --build migrate
docker compose up -d webapi
```

---

## ข้อควรระวัง

- อย่าให้หลาย stack ชี้ฐานข้อมูลเดียวกันแล้วรัน migration พร้อมกัน
- ระวัง env production: ต้องยืนยันว่า connection string ของ `migrate` ชี้ DB ที่ถูกต้อง
- ถ้าใช้ compose รุ่นเก่าที่ไม่รองรับ `service_completed_successfully` ให้ใช้ script entrypoint ใน `webapi` ตรวจสถานะ migration แทน

---

## Recommendation สำหรับโปรเจกต์นี้

สำหรับโปรเจกต์ ecommerce นี้ ให้ใช้แนวทางต่อไปนี้เป็น baseline:

1. คง `db` healthcheck ที่มีอยู่
2. เพิ่ม `migrate` service แบบ one-shot
3. ให้ `webapi` รอ `migrate` สำเร็จ
4. ปิดการ migrate อัตโนมัติใน app runtime
5. ค่อย optimize ไป EF bundle เมื่อ flow เสถียร

แนวทางนี้สมดุลระหว่างมาตรฐาน, ความปลอดภัยของข้อมูล, และประสิทธิภาพในการรันผ่าน Docker Compose คำสั่งเดียว
