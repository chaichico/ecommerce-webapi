# Database Setup Guide

คู่มือการตั้งค่า Database ให้รองรับทั้ง **Local SQL Server** และ **Docker SQL Server** โดยไม่ hard code ใน project

---

## 🎯 เป้าหมาย

- สามารถเลือกใช้ Local SQL Server หรือ Docker SQL Server ได้
- ใช้ Environment Variables (.env) ในการกำหนดค่า
- ไม่มี connection string hard code ใน project
- สลับ environment ได้ง่าย

---

## 📋 ขั้นตอนการตั้งค่า

### Step 1: สร้างไฟล์ .env

สร้างไฟล์ `.env` ใน root directory (ไฟล์นี้จะไม่ถูก commit เข้า git)

```env
# Environment Mode
ASPNETCORE_ENVIRONMENT=Development

# Database Configuration
DB_SERVER=localhost
DB_PORT=1433
DB_NAME=EcommerceDb
DB_USER=sa
DB_PASSWORD=YourStrong!Pass123

# Admin Credentials (for Basic Auth)
ADMIN_USERNAME=admin
ADMIN_PASSWORD=Admin@123

# JWT Configuration
JWT_SECRET=YourSuperSecretKeyForJWT_MinimumLength32Characters!
JWT_ISSUER=EcommerceAPI
JWT_AUDIENCE=EcommerceClient
JWT_EXPIRY_MINUTES=60

# Encryption Key (for Phone Number)
ENCRYPTION_KEY=YourEncryptionKey32CharactersLong!
```

---

### Step 2: สร้างไฟล์ .env.example

สร้างไฟล์ `.env.example` เป็น template (ไฟล์นี้จะถูก commit เข้า git)

```env
# Environment Mode
ASPNETCORE_ENVIRONMENT=Development

# Database Configuration
# For Local SQL Server: DB_SERVER=localhost or .\SQLEXPRESS
# For Docker SQL Server: DB_SERVER=db
DB_SERVER=localhost
DB_PORT=1433
DB_NAME=EcommerceDb
DB_USER=sa
DB_PASSWORD=YourStrong!Pass123

# Admin Credentials (for Basic Auth)
ADMIN_USERNAME=admin
ADMIN_PASSWORD=Admin@123

# JWT Configuration
JWT_SECRET=YourSuperSecretKeyForJWT_MinimumLength32Characters!
JWT_ISSUER=EcommerceAPI
JWT_AUDIENCE=EcommerceClient
JWT_EXPIRY_MINUTES=60

# Encryption Key (for Phone Number - must be 32 characters)
ENCRYPTION_KEY=YourEncryptionKey32CharactersLong!
```

---

### Step 3: แก้ไฟล์ appsettings.json

ลบ connection string ออก (จะใช้จาก .env แทน)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

### Step 4: แก้ไฟล์ Program.cs

เพิ่มการอ่าน .env และสร้าง connection string แบบ dynamic

```csharp
using Microsoft.EntityFrameworkCore;
using Data;
using Controllers;
using DotNetEnv; // ต้องติดตั้ง package: dotnet add package DotNetEnv

var builder = WebApplication.CreateBuilder(args);

// Load .env file
Env.Load();

// Build Connection String from Environment Variables
var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "EcommerceDb";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "YourStrong!Pass123";

var connectionString = $"Server={dbServer},{dbPort};Database={dbName};User={dbUser};Password={dbPassword};TrustServerCertificate=True;";

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// Controller
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

### Step 5: ติดตั้ง Package ที่จำเป็น

```bash
dotnet add package DotNetEnv
```

---

## 🔧 การใช้งาน

### Option 1: ใช้ Local SQL Server

1. แก้ไฟล์ `.env`:
```env
DB_SERVER=localhost
# หรือถ้าใช้ SQL Server Express
# DB_SERVER=.\SQLEXPRESS
DB_PORT=1433
DB_NAME=EcommerceDb
DB_USER=sa
DB_PASSWORD=YourLocalPassword
```

2. ตรวจสอบว่า SQL Server ทำงานอยู่:
```bash
# Windows Services
services.msc
# หา SQL Server (MSSQLSERVER) หรือ SQL Server (SQLEXPRESS)
```

3. Run Migration:
```bash
dotnet ef database update
```

4. Run Application:
```bash
dotnet run
```

---

### Option 2: ใช้ Docker SQL Server

1. แก้ไฟล์ `.env`:
```env
DB_SERVER=db
DB_PORT=1433
DB_NAME=EcommerceDb
DB_USER=sa
DB_PASSWORD=YourStrong!Pass123
```

2. Start Docker Containers:
```bash
docker-compose up -d
```

3. รอ SQL Server พร้อมใช้งาน (ประมาณ 10-30 วินาที):
```bash
docker logs sqlserver
# รอจนเห็น "SQL Server is now ready for client connections"
```

4. Run Migration (จากเครื่อง host):
```bash
dotnet ef database update
```

5. Run Application ใน Docker:
```bash
# Rebuild และ restart
docker-compose down
docker-compose up --build
```

หรือ Run แบบ local แต่เชื่อม Docker DB:
```bash
# แก้ DB_SERVER=localhost ใน .env
# แล้ว run
dotnet run
```

---

## 🐳 Docker Compose Configuration

ตรวจสอบว่า `docker-compose.yml` มีการ pass environment variables:

```yaml
version: '3.8'

services:
  webapi:
    build: .
    container_name: ecommerce-api
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
      - DB_SERVER=db
      - DB_PORT=1433
      - DB_NAME=EcommerceDb
      - DB_USER=sa
      - DB_PASSWORD=YourStrong!Pass123
    depends_on:
      - db

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

volumes:
  sqlserver_data:
```

---

## 🧪 การทดสอบ Connection

### ทดสอบ Local SQL Server

```bash
# ใช้ sqlcmd (ถ้ามี)
sqlcmd -S localhost -U sa -P YourLocalPassword -Q "SELECT @@VERSION"

# หรือใช้ SQL Server Management Studio (SSMS)
# Server name: localhost หรือ .\SQLEXPRESS
```

### ทดสอบ Docker SQL Server

```bash
# เข้าไปใน container
docker exec -it sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong!Pass123

# Query
SELECT @@VERSION
GO

# ออกจาก sqlcmd
exit
```

---

## 📝 Checklist

### สำหรับ Local Development
- [x] สร้างไฟล์ `.env` จาก `.env.example`
- [x] แก้ `DB_SERVER=localhost` (หรือ `.\SQLEXPRESS`)
- [x] ติดตั้ง package `DotNetEnv`
- [x] แก้ `Program.cs` ให้อ่าน environment variables
- [x] ลบ connection string จาก `appsettings.json`
- [ ] Run `dotnet ef database update`
- [ ] Run `dotnet run`
- [ ] ทดสอบ API ที่ http://localhost:5000/swagger

### สำหรับ Docker
- [x] สร้างไฟล์ `.env` จาก `.env.example`
- [x] แก้ `DB_SERVER=db` (ใน docker-compose.yml)
- [x] Update `docker-compose.yml` ให้ pass environment variables
- [x] เพิ่ม volume สำหรับ SQL Server data persistence
- [ ] Run `docker-compose up -d`
- [ ] รอ SQL Server พร้อม (ดู logs)
- [ ] Run `dotnet ef database update` (จาก host)
- [ ] Rebuild: `docker-compose up --build`
- [ ] ทดสอบ API ที่ http://localhost:8080/swagger

### สำหรับส่งงาน
- [x] ตรวจสอบว่า `.env` อยู่ใน `.gitignore`
- [x] มีไฟล์ `.env.example` พร้อม comments
- [ ] เขียน README.md อธิบายวิธี setup
- [ ] ทดสอบ `docker-compose up` ใหม่ทั้งระบบ
- [ ] ทดสอบทุก API endpoints

---

## ⚠️ หมายเหตุสำคัญ

1. **ไฟล์ .env ห้าม commit เข้า git** - มี sensitive data
2. **ไฟล์ .env.example ต้อง commit** - เป็น template
3. **Password ต้องแข็งแรง** - ตาม SQL Server requirements (ตัวพิมพ์ใหญ่, เล็ก, ตัวเลข, อักขระพิเศษ)
4. **JWT_SECRET ต้องยาวพอ** - อย่างน้อย 32 characters
5. **ENCRYPTION_KEY ต้องยาวพอดี** - ต้อง 32 characters สำหรับ AES-256

---

## 🔍 Troubleshooting

### ปัญหา: Connection ไม่ได้
- ตรวจสอบ SQL Server ทำงานอยู่หรือไม่
- ตรวจสอบ port 1433 ว่าง
- ตรวจสอบ firewall
- ตรวจสอบ password ถูกต้อง

### ปัญหา: Migration ไม่ผ่าน
- ตรวจสอบ connection string
- ลอง run `dotnet ef migrations list`
- ลอง drop database แล้วสร้างใหม่

### ปัญหา: Docker SQL Server ไม่ start
- ตรวจสอบ password ตาม requirements
- ดู logs: `docker logs sqlserver`
- ลอง remove container: `docker-compose down -v`

---

**อัพเดทล่าสุด:** 21 เมษายน 2026
