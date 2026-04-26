# E-commerce API Development Checklist

## 📋 Technical Requirements

### 1. Web API Development (.NET 10 + ORM)
- [x] สร้างโปรเจกต์ .NET 10 Web API
- [x] ติดตั้งและกำหนดค่า Entity Framework Core
- [x] เชื่อมต่อกับ SQL Server Database
- [x] สร้าง DbContext (AppDbContext)

### 2. REST API ตาม OpenAPI Specification
- [x] ติดตั้ง Swagger/OpenAPI
- [x] กำหนดค่า Swagger ใน Program.cs (รวม Bearer token support)
- [x] สร้าง API Controllers ตาม spec ครบถ้วน (UsersController, OrdersController, AdminController)
- [ ] เพิ่ม API Documentation/Comments (XML comments ยังไม่มี)

### 3. Environment Variables & Configuration
- [x] สร้างไฟล์ .gitignore
- [ ] สร้างไฟล์ .env.example พร้อม template (ไม่มี — ใช้ appsettings.json แทน)
- [x] กำหนดค่า Connection String แบบ dynamic จาก appsettings.json / ENV
- [x] เพิ่มตัวแปร Admin credentials ใน appsettings.json (AdminAuth section)
- [x] เพิ่มตัวแปร JWT configuration ใน appsettings.json (Jwt section)
- [x] เพิ่มตัวแปร Encryption Key ใน appsettings.json (Encryption section)
- [ ] ติดตั้ง DotNetEnv package (ไม่ได้ใช้ — ใช้ appsettings.json แทน)
- [ ] แก้ไข Program.cs ให้อ่าน .env file (ไม่ได้ทำ — ใช้ appsettings.json แทน)

### 4. Dockerfile
- [x] สร้าง Dockerfile
- [x] ใช้ Multi-stage build (SDK + Runtime)
- [x] เลือก Base Image ที่เหมาะสม (aspnet:10.0)
- [x] Optimize Docker layers

### 5. Docker Compose
- [x] สร้าง docker-compose.yml
- [x] กำหนด Web API ให้ run ที่ port 8080
- [x] เพิ่ม SQL Server service
- [x] กำหนด dependencies ระหว่าง services
- [x] Pass environment variables ไปยัง containers
- [x] เพิ่ม volume สำหรับ SQL Server data persistence

### 6. JWT Authentication & Authorization
- [x] ติดตั้ง JWT packages (Microsoft.AspNetCore.Authentication.JwtBearer)
- [x] สร้าง JWT Token generation (GenerateJwtToken ใน UserService)
- [x] กำหนดค่า JWT ใน Program.cs (AddAuthentication + AddJwtBearer)
- [x] เพิ่ม JWT Secret ใน appsettings.json (Jwt:Key)
- [x] Implement JWT Token generation ใน Login API
- [x] เพิ่ม [Authorize] attribute ใน protected endpoints (OrdersController)

---

## 🌟 Optional Requirements (คะแนนพิเศษ)

### 1. Automated Testing Support
- [ ] สร้างโครงสร้าง Test Project
- [ ] กำหนดค่า InMemory Database สำหรับ Testing
- [ ] เพิ่มตัวแปร ENV RunMode = test
- [ ] เขียน Unit Tests
- [ ] เขียน Integration Tests

### 2. Code First & Database Migration
- [x] ใช้ Code First approach
- [x] สร้าง Migration files
- [x] สร้าง Initial Migration (20260417090318_InitialCreate)
- [x] เพิ่ม Migration สำหรับ features ใหม่ (20260423060405_FixOrderItemProductId)

---

## 💼 Business Requirements

### Database Models
- [x] สร้าง User Model
- [x] สร้าง Product Model
- [x] สร้าง Order Model
- [x] สร้าง OrderItem Model
- [x] กำหนด Relationships ระหว่าง Models
- [x] เพิ่ม Data Annotations และ Validations

### 1. Seed Data Function
- [x] สร้าง Seed Data Service (DbSeeder.cs)
- [x] Seed Product Items (Auto)
- [x] Seed Product Status Reference (Auto)
- [x] ตรวจสอบ empty collection ก่อน seed

### 2. User Registration (API Create User)
- [x] สร้าง UsersController
- [x] สร้าง RegisterUserDto
- [x] Implement POST /api/users/register
- [x] Validate Email (unique, format)
- [x] Validate FirstName, LastName (required)
- [x] Validate PhoneNumber (optional)
- [x] **[คะแนนพิเศษ]** Encrypt PhoneNumber (Symmetric Encryption via EncryptionService)
- [x] **[คะแนนพิเศษ]** Hash Password (Hashing via PasswordHasher)
- [x] **[คะแนนพิเศษ]** Validate ConfirmPassword ([Compare] annotation)

### 3. User Login (API User Login)
- [x] สร้าง LoginDto
- [x] Implement POST /api/users/login
- [x] Validate Email & Password
- [x] Generate JWT Token
- [x] Include User Info in JWT Payload (Email, FirstName, LastName)
- [x] Return Token + User Info (LoginResponseDto)

### 4. Admin Search Orders (API Admin Search Order List)
- [x] สร้าง AdminController
- [x] Implement Basic Authentication (IsAuthorized helper ใน AdminController)
- [x] เพิ่ม Admin credentials ใน appsettings.json (AdminAuth section)
- [x] Implement GET /api/admin/orders
- [x] Search by OrderNumber
- [x] Search by User FirstName, LastName
- [x] Return Order List with Status (รอยืนยัน, ยืนยันแล้ว)
- [x] Include Order Details in response (AdminOrderResponseDto)

### 5. User Create Order (API User Create Order)
- [x] สร้าง CreateOrderDto + CreateOrderItemDto
- [x] Implement POST /api/orders
- [x] เพิ่ม JWT Authentication ([Authorize] บน OrdersController)
- [x] รองรับหลาย Products ในคำสั่งซื้อเดียว
- [x] Validate Product Number & Quantity (ตรวจ product active + exists)
- [x] Generate OrderNumber (format: ORD-yyyyMMdd-XXXXXXXX)
- [x] Return OrderNumber + Order Details (OrderResponseDto)

### 6. User Update Order (API User Update Order)
- [x] สร้าง UpdateOrderDto
- [x] Implement PUT /api/orders/{id}
- [x] เพิ่ม JWT Authentication
- [x] Validate Order exists
- [x] Update Product Number & Quantity
- [x] ตรวจสอบ Order ownership (User เป็นเจ้าของ Order)

### 7. User Confirm Order (API User Confirm Order)
- [x] สร้าง ConfirmOrderDto
- [x] Implement POST /api/orders/{id}/confirm
- [x] เพิ่ม JWT Authentication
- [x] Validate Order exists
- [x] เพิ่ม ShippingAddress
- [x] Update Order Status

### 8. Admin Approve Orders (API Admin Approve Order)
- [x] สร้าง ApproveOrdersDto
- [x] Implement POST /api/admin/orders/approve
- [x] เพิ่ม Basic Authentication
- [x] รองรับการ approve หลาย Orders พร้อมกัน
- [x] Update Status เป็น "ยืนยันคำสั่งซื้อ"

---

## 🏗️ Architecture & Code Structure

### DTOs (Data Transfer Objects)
- [x] สร้าง RegisterUserDto
- [x] สร้าง LoginDto + LoginResponseDto
- [x] สร้าง CreateOrderDto + CreateOrderItemDto
- [x] สร้าง UpdateOrderDto
- [x] สร้าง ConfirmOrderDto
- [x] สร้าง ApproveOrdersDto
- [x] สร้าง Response DTOs (OrderResponseDto, UserResponseDto, AdminOrderResponseDto)

### Services Layer
- [x] สร้าง IUserService + UserService (รวม JWT generation)
- [ ] สร้าง IAuthService + AuthService (Auth รวมอยู่ใน UserService แทน)
- [x] สร้าง IOrderService + OrderService
- [ ] สร้าง IProductService + ProductService (ไม่ได้สร้าง — access ผ่าน DbContext โดยตรง)
- [ ] สร้าง IJwtService + JwtService (JWT รวมอยู่ใน UserService แทน)
- [x] สร้าง IEncryptionService + EncryptionService
- [x] สร้าง IPasswordHasher + PasswordHasher

### Repositories Layer
- [x] สร้าง IUserRepository + UserRepository
- [x] สร้าง IOrderRepository + OrderRepository
- [ ] สร้าง IProductRepository + ProductRepository (ไม่ได้สร้าง)
- [ ] Implement Generic Repository Pattern (optional)

### Middleware
- [ ] สร้าง Basic Authentication Middleware (ใช้ IsAuthorized() helper method ใน AdminController แทน)
- [ ] สร้าง Error Handling Middleware
- [ ] สร้าง Logging Middleware (optional)

---

## ✅ Testing & Validation

- [x] ทดสอบ User Registration
- [x] ทดสอบ User Login + JWT
- [x] ทดสอบ Create Order
- [x] ทดสอบ Update Order
- [x] ทดสอบ Confirm Order
- [x] ทดสอบ Admin Search Orders
- [x] ทดสอบ Admin Approve Orders
- [x] ทดสอบ Basic Authentication
- [x] ทดสอบ JWT Authentication
- [x] ทดสอบ Seed Data
- [ ] ทดสอบ Docker Build
- [ ] ทดสอบ Docker Compose

---

## 📝 Documentation

- [x] เขียน README.md
- [x] เขียน API Documentation (docs/ folder: api-*.md)
- [ ] เพิ่ม Setup Instructions ใน README
- [ ] เขียน Environment Variables Guide
- [ ] เขียน Docker Instructions

---

## 🎯 Progress Summary

**เสร็จแล้ว:**
- ✅ Database Models (User, Product, Order, OrderItem)
- ✅ DbContext และ Relationships
- ✅ Database Migration (Initial)
- ✅ Dockerfile (Multi-stage, optimized)
- ✅ Docker Compose (Port 8080, SQL Server)
- ✅ Swagger/OpenAPI Setup
- ✅ .gitignore
- ✅ Seed Data Function (DbSeeder)

**กำลังดำเนินการ:**
- 🔄 Controllers (มีเพียง UsersController แบบ basic)

**ยังไม่ได้ทำ:**
- ❌ DTOs
- ❌ Services Layer
- ❌ Repositories Layer
- ❌ JWT Authentication
- ❌ Basic Authentication
- ❌ All Business Logic APIs
- ❌ Encryption (Phone, Password)
- ❌ .env.example configuration
- ❌ Testing Infrastructure

---

