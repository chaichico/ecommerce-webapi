# E-commerce API Development Checklist

อัปเดตล่าสุด: 29 เมษายน 2026

## สถานะภาพรวม

- [x] Business APIs หลักครบ (User + Order + Admin)
- [x] โครงสร้างตามชั้น Controller -> Service -> Repository -> DbContext
- [x] ใช้ DTO และ Interface แยกชัดเจน
- [x] ใช้ EF Core Code First + Migration
- [x] JWT (user) และ Basic Auth (admin) ทำงานแล้ว
- [x] มี Unit Tests ฝั่งหลัก
- [x] มีระบบ High-Performance Logging (Serilog + Channel + Background Service)
- [ ] ยังขาดงานด้านคุณภาพระบบบางส่วน (integration test, middleware กลาง, docs เพิ่มเติม)

## Technical Requirements

### 1. Web API + Database
- [x] .NET 10 Web API
- [x] EF Core + SQL Server
- [x] AppDbContext และ Entity Configurations
- [x] DbSeeder สำหรับ seed data

### 2. API / REST
- [x] มี Controllers หลัก: Users, Orders, Admin
- [x] เปิด Swagger/OpenAPI แล้ว
- [x] Endpoint สร้างข้อมูลหลักตอบ `201 Created` แล้ว (`register`, `create order`)
- [ ] เพิ่ม XML comments ให้ Swagger แสดงรายละเอียดครบ
- [ ] พิจารณาปรับ `CreatedAtAction(null, result)` ให้ชี้ route จริงเพื่อให้ `Location` header meaningful

### 3. Configuration / Security
- [x] ใช้ appsettings + environment variables
- [x] ตั้งค่า `Jwt`, `Encryption`, `ConnectionStrings`, `AdminAuth`
- [x] เข้ารหัสเบอร์โทร (symmetric encryption)
- [x] Hash รหัสผ่าน (one-way hashing)
- [x] ลบ/ยุบ config ที่ซ้ำซ้อนใน `appsettings.json` (เช่น `AdminCredentials` ถ้าไม่ใช้)

### 4. Docker
- [x] มี `Dockerfile` แบบ multi-stage
- [x] มี `docker-compose.yml` พร้อม SQL Server
- [x] ทดสอบ `docker compose up --build` แบบ end-to-end ล่าสุด

### 5. Logging & Monitoring
- [x] ติดตั้ง Serilog packages (AspNetCore, Sinks.File, Sinks.Async, Enrichers.Environment, Enrichers.Thread)
- [x] สร้าง LogEntry model
- [x] สร้าง ILogChannel และ LogChannel (Channel-based non-blocking)
- [x] สร้าง LogBackgroundService (Serilog consumer)
- [x] สร้าง LogSummaryService (PeriodicTimer + ConcurrentDictionary)
- [x] สร้าง LoggingMiddleware (ดักจับ Request/Response)
- [x] Configure Serilog ใน Program.cs (JSON formatter, rolling daily, retain 30 วัน)
- [x] Register logging services เป็น Singleton และ HostedService
- [x] เพิ่ม LoggingMiddleware ก่อน UseAuthentication
- [x] Build ผ่านไม่มี error
- [ ] ทดสอบรัน API และตรวจสอบไฟล์ `logs/audit-YYYYMMDD.json`
- [ ] ทดสอบรอ 5 นาทีและตรวจสอบไฟล์ `logs/summary-YYYYMMDD.json`
- [ ] ทดสอบ Graceful Shutdown
- [ ] ตรวจสอบ performance overhead (ElapsedMs ไม่เพิ่มมากกว่า 1 ms)

## Business Requirements

### User APIs
- [x] Register (`POST /api/users/register`)
- [x] Login (`POST /api/users/login`)

### Order APIs
- [x] Create order (`POST /api/orders`)
- [x] Update order (`PUT /api/orders/{id}`)
- [x] Confirm order (`POST /api/orders/{id}/confirm`)

### Admin APIs
- [x] Search orders (`GET /api/admin/orders`)
- [x] Approve orders (`POST /api/admin/orders/approve`)

## Architecture & Code Structure

### Layers
- [x] Controllers -> Services -> Repositories แยกหน้าที่ชัด
- [x] Inject ผ่าน interface

### DTOs / Interfaces
- [x] DTO แยก request/response
- [x] Service interfaces ครบชุดหลัก
- [x] Repository interfaces ครบ (`IUserRepository`, `IOrderRepository`, `IProductRepository`)

### Domain Model
- [x] มี `OrderStatus` แบบ enum แล้ว
- [x] Migration สำหรับ enum conversion แล้ว

## Testing & Validation

- [x] Test project (`Ecommerce.Tests`) พร้อม InMemory DB
- [x] Unit tests ครอบคลุม repository/service หลัก
- [ ] Integration tests ระดับ API
- [ ] Regression test สำหรับ flow docker + migration

## Documentation

- [x] มี `README.md`
- [x] มีเอกสารใน `docs/` หลายส่วน
- [ ] เพิ่มคู่มือ Environment Variables แบบแยกหัวข้อ
- [ ] เพิ่มคู่มือ Docker troubleshooting แบบสั้นใน README

## Next Priorities

- [x] เพิ่มระบบ High-Performance Logging (Serilog + Channel + Background Service) ✅
- [ ] ทดสอบระบบ Logging ใน production-like environment
- [ ] เพิ่ม global exception handling middleware + error response format เดียวทั้งระบบ (`{ "message": "..." }`)
- [ ] เพิ่ม integration tests สำหรับเส้นทางสำคัญ (`register`, `login`, `create/update/confirm order`, `admin approve`)
- [ ] ปรับ `CreatedAtAction` ให้ผูกกับ GET endpoint จริงหรือใช้ `StatusCode(201, result)` ให้สม่ำเสมอ
- [ ] cleanup config ที่ไม่ใช้งาน

