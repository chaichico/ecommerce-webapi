# API Checklist

รายการ API ทั้งหมดที่ต้องพัฒนาตาม Specification

## 📋 User APIs (ใช้ JWT Authentication)

### 1. User Registration [x]
- **Endpoint**: `POST /api/users/register`
- **Authentication**: ไม่ต้อง
- **Description**: ลงทะเบียนผู้ใช้งานใหม่
- **Request Body**:
  - Email (required, unique)
  - ชื่อ (required)
  - นามสกุล (required)
  - เบอร์โทร (optional, encrypted)
  - Password (required, hashed)
  - ConfirmPassword (required, hashed)
- **Status**: ✅ เสร็จแล้ว

### 2. User Login [x]
- **Endpoint**: `POST /api/users/login`
- **Authentication**: ไม่ต้อง
- **Description**: เข้าสู่ระบบด้วย Email และ Password
- **Request Body**:
  - Email
  - Password
- **Response**: JWT Token พร้อม User Info (Email, ชื่อ, นามสกุล)
- **Status**: ✅ เสร็จแล้ว

### 3. Create Order []
- **Endpoint**: `POST /api/orders`
- **Authentication**: JWT Token (required)
- **Description**: สร้างคำสั่งซื้อสินค้าใหม่
- **Request Body**:
  - Order Details (array):
    - Product Number
    - จำนวน item
- **Response**: OrderNumber
- **Status**: ⬜ ยังไม่เสร็จ

### 4. Update Order []
- **Endpoint**: `PUT /api/orders/{orderNumber}`
- **Authentication**: JWT Token (required)
- **Description**: แก้ไขคำสั่งซื้อสินค้า
- **Request Body**:
  - Product Number
  - จำนวน
- **Status**: ⬜ ยังไม่เสร็จ

### 5. Confirm Order []
- **Endpoint**: `POST /api/orders/{orderNumber}/confirm`
- **Authentication**: JWT Token (required)
- **Description**: ยืนยันคำสั่งซื้อสินค้า
- **Request Body**:
  - ที่อยู่จัดส่ง
- **Status**: ⬜ ยังไม่เสร็จ

---

## 🔐 Admin APIs (ใช้ Basic Authentication)

### 1. Search Order List []
- **Endpoint**: `GET /api/admin/orders`
- **Authentication**: Basic Authentication (required)
- **Description**: ค้นหาและดูรายการคำสั่งซื้อทั้งหมด
- **Query Parameters**:
  - OrderNumber (optional)
  - ชื่อ (optional)
  - นามสกุล (optional)
- **Response**: Order List พร้อมสถานะ (รอยืนยันคำสั่งซื้อ, ยืนยันคำสั่งซื้อ) และ Order Details
- **Status**: ⬜ ยังไม่เสร็จ

### 2. Approve Orders []
- **Endpoint**: `POST /api/admin/orders/approve`
- **Authentication**: Basic Authentication (required)
- **Description**: อนุมัติคำสั่งซื้อหลายรายการพร้อมกัน
- **Request Body**:
  - OrderNumbers (array)
- **Response**: สถานะการอนุมัติ
- **Status**: ⬜ ยังไม่เสร็จ

---

## 📊 สรุป

- **User APIs**: 5 endpoints
- **Admin APIs**: 2 endpoints
- **รวมทั้งหมด**: 7 endpoints

## 📝 หมายเหตุ

- User APIs ใช้ JWT Token สำหรับ Authentication และ Authorization
- Admin APIs ใช้ Basic Authentication (username และ password จาก ENV)
- ต้องมี Function Seed Data สำหรับ Product และ Product Status Reference
- Password ต้อง Hash ด้วย Hashing Encryption
- เบอร์โทรต้อง Encrypt ด้วย Symmetric Encryption (optional)
