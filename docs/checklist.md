# E-commerce API Development Checklist

## 📋 Technical Requirements

### 1. Web API Development (.NET 10 + ORM)
- [x] สร้างโปรเจกต์ .NET 10 Web API
- [x] ติดตั้งและกำหนดค่า Entity Framework Core
- [x] เชื่อมต่อกับ SQL Server Database
- [x] สร้าง DbContext (AppDbContext)

### 2. REST API ตาม OpenAPI Specification
- [x] ติดตั้ง Swagger/OpenAPI
- [x] กำหนดค่า Swagger ใน Program.cs
- [ ] สร้าง API Controllers ตาม spec ครบถ้วน
- [ ] เพิ่ม API Documentation/Comments

### 3. Environment Variables & Configuration
- [x] สร้างไฟล์ .gitignore
- [x] สร้างไฟล์ .env.example พร้อม template
- [x] กำหนดค่า Connection String แบบ dynamic จาก ENV
- [x] เพิ่มตัวแปร Admin credentials ใน ENV
- [x] เพิ่มตัวแปร JWT configuration ใน ENV
- [x] เพิ่มตัวแปร Encryption Key ใน ENV
- [x] ติดตั้ง DotNetEnv package
- [x] แก้ไข Program.cs ให้อ่าน .env file

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
- [ ] ติดตั้ง JWT packages
- [ ] สร้าง JWT Token Service
- [ ] กำหนดค่า JWT ใน Program.cs
- [ ] เพิ่ม JWT Secret ใน ENV
- [ ] Implement JWT Token generation ใน Login API
- [ ] เพิ่ม [Authorize] attribute ใน protected endpoints

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
- [x] สร้าง Initial Migration
- [ ] เพิ่ม Migration สำหรับ features ใหม่

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
- [ ] สร้าง Seed Data Service
- [ ] Seed Product Items (Auto)
- [ ] Seed Product Status Reference (Auto)
- [ ] ตรวจสอบ empty collection ก่อน seed

### 2. User Registration (API Create User)
- [ ] สร้าง UserController/AuthController
- [ ] สร้าง RegisterDTO
- [ ] Implement POST /api/users/register
- [ ] Validate Email (unique, format)
- [ ] Validate FirstName, LastName (required)
- [ ] Validate PhoneNumber (optional)
- [ ] **[คะแนนพิเศษ]** Encrypt PhoneNumber (Symmetric Encryption)
- [ ] **[คะแนนพิเศษ]** Hash Password (Hashing)
- [ ] **[คะแนนพิเศษ]** Validate ConfirmPassword

### 3. User Login (API User Login)
- [ ] สร้าง LoginDTO
- [ ] Implement POST /api/auth/login
- [ ] Validate Email & Password
- [ ] Generate JWT Token
- [ ] Include User Info in JWT Payload (Email, FirstName, LastName)
- [ ] Return Token + User Info

### 4. Admin Search Orders (API Admin Search Order List)
- [ ] สร้าง OrdersController (Admin)
- [ ] Implement Basic Authentication Middleware
- [ ] เพิ่ม Admin credentials ใน ENV
- [ ] Implement GET /api/admin/orders
- [ ] Search by OrderNumber
- [ ] Search by User FirstName, LastName
- [ ] Return Order List with Status (รอยืนยัน, ยืนยันแล้ว)
- [ ] Include Order Details in response

### 5. User Create Order (API User Create Order)
- [ ] สร้าง CreateOrderDTO
- [ ] Implement POST /api/orders
- [ ] เพิ่ม JWT Authentication
- [ ] รองรับหลาย Products ในคำสั่งซื้อเดียว
- [ ] Validate Product Number & Quantity
- [ ] Generate OrderNumber
- [ ] Return OrderNumber + Order Details

### 6. User Update Order (API User Update Order)
- [ ] สร้าง UpdateOrderDTO
- [ ] Implement PUT /api/orders/{orderNumber}
- [ ] เพิ่ม JWT Authentication
- [ ] Validate OrderNumber
- [ ] Update Product Number & Quantity
- [ ] ตรวจสอบ Order ownership (User เป็นเจ้าของ Order)

### 7. User Confirm Order (API User Confirm Order)
- [ ] สร้าง ConfirmOrderDTO
- [ ] Implement POST /api/orders/{orderNumber}/confirm
- [ ] เพิ่ม JWT Authentication
- [ ] Validate OrderNumber
- [ ] เพิ่ม ShippingAddress
- [ ] Update Order Status

### 8. Admin Approve Orders (API Admin Approve Order)
- [ ] สร้าง ApproveOrdersDTO
- [ ] Implement POST /api/admin/orders/approve
- [ ] เพิ่ม Basic Authentication
- [ ] รองรับการ approve หลาย Orders พร้อมกัน
- [ ] Update Status เป็น "ยืนยันคำสั่งซื้อ"

---

## 🏗️ Architecture & Code Structure

### DTOs (Data Transfer Objects)
- [ ] สร้าง RegisterDTO
- [ ] สร้าง LoginDTO
- [ ] สร้าง CreateOrderDTO
- [ ] สร้าง UpdateOrderDTO
- [ ] สร้าง ConfirmOrderDTO
- [ ] สร้าง ApproveOrdersDTO
- [ ] สร้าง Response DTOs

### Services Layer
- [ ] สร้าง IUserService + UserService
- [ ] สร้าง IAuthService + AuthService
- [ ] สร้าง IOrderService + OrderService
- [ ] สร้าง IProductService + ProductService
- [ ] สร้าง IJwtService + JwtService
- [ ] สร้าง IEncryptionService + EncryptionService (optional)

### Repositories Layer
- [ ] สร้าง IUserRepository + UserRepository
- [ ] สร้าง IOrderRepository + OrderRepository
- [ ] สร้าง IProductRepository + ProductRepository
- [ ] Implement Generic Repository Pattern (optional)

### Middleware
- [ ] สร้าง Basic Authentication Middleware
- [ ] สร้าง Error Handling Middleware
- [ ] สร้าง Logging Middleware (optional)

---

## ✅ Testing & Validation

- [ ] ทดสอบ User Registration
- [ ] ทดสอบ User Login + JWT
- [ ] ทดสอบ Create Order
- [ ] ทดสอบ Update Order
- [ ] ทดสอบ Confirm Order
- [ ] ทดสอบ Admin Search Orders
- [ ] ทดสอบ Admin Approve Orders
- [ ] ทดสอบ Basic Authentication
- [ ] ทดสอบ JWT Authentication
- [ ] ทดสอบ Seed Data
- [ ] ทดสอบ Docker Build
- [ ] ทดสอบ Docker Compose

---

## 📝 Documentation

- [ ] เขียน README.md
- [ ] เขียน API Documentation
- [ ] เขียน Setup Instructions
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

**กำลังดำเนินการ:**
- 🔄 Controllers (มีเพียง UsersController แบบ basic)

**ยังไม่ได้ทำ:**
- ❌ DTOs
- ❌ Services Layer
- ❌ Repositories Layer
- ❌ JWT Authentication
- ❌ Basic Authentication
- ❌ Seed Data
- ❌ All Business Logic APIs
- ❌ Encryption (Phone, Password)
- ❌ .env.example configuration
- ❌ Testing Infrastructure

---

**อัพเดทล่าสุด:** 21 เมษายน 2026
