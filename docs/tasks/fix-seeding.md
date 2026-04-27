# Fix Database Seeding - Password Hashing & Encryption

## ปัญหาที่พบ
- ❌ DbSeeder เก็บ password เป็น plain text (`"hashed_password_1"`) ไม่ใช่ hash จริง
- ❌ เบอร์โทรศัพท์ไม่ได้ encrypt
- ❌ ตอน login ระบบ hash password แล้วเทียบกับ plain text → ไม่ตรงกัน
- ❌ Login ไม่สำเร็จ

## สิ่งที่แก้ไข

### 1. แก้ไฟล์ `Data/DbSeeder.cs`
- [x] เพิ่ม `using Interfaces;`
- [x] เพิ่ม parameters: `IPasswordHasher passwordHasher, IEncryptionService encryptionService`
- [x] เปลี่ยน method signature:
  ```csharp
  public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher, IEncryptionService encryptionService)
  ```
- [x] แก้ password ทั้ง 3 users:
  - จาก: `PasswordHash = "hashed_password_1"`
  - เป็น: `PasswordHash = passwordHasher.HashPassword("Password123")`
- [x] แก้เบอร์โทรทั้ง 3 users:
  - จาก: `PhoneNumber = "081-234-5678"`
  - เป็น: `PhoneNumber = encryptionService.Encrypt("081-234-5678")`

### 2. แก้ไฟล์ `Program.cs`
- [x] เพิ่มการดึง services จาก DI container:
  ```csharp
  var passwordHasher = services.GetRequiredService<IPasswordHasher>();
  var encryptionService = services.GetRequiredService<IEncryptionService>();
  ```
- [x] ส่ง services เข้าไปใน DbSeeder:
  ```csharp
  await DbSeeder.SeedAsync(context, passwordHasher, encryptionService);
  ```

## ข้อมูล Test Users หลัง Fix

| Email | Password | Phone (Encrypted) |
|-------|----------|-------------------|
| john.doe@example.com | Password123 | 081-234-5678 |
| jane.smith@example.com | Password123 | 082-345-6789 |
| somchai.thai@example.com | Password123 | 083-456-7890 |

## ขั้นตอนการทดสอบ

### 1. ลบ Database เดิม
```bash
# ถ้าใช้ Docker
docker-compose down -v
docker-compose up -d

# หรือใช้ EF Core
dotnet ef database drop --force
dotnet ef database update
```

### 2. Run Application
```bash
dotnet run
```

### 3. ทดสอบ Login ใน Swagger
- เปิด Swagger UI: `https://localhost:5001/swagger`
- ไปที่ `POST /api/users/login`
- กรอก:
  ```json
  {
    "email": "john.doe@example.com",
    "password": "Password123"
  }
  ```
- ✅ ควรได้ JWT Token กลับมา

### 4. ทดสอบ Register User ใหม่
- ไปที่ `POST /api/users/register`
- กรอก:
  ```json
  {
    "email": "test@example.com",
    "firstName": "Test",
    "lastName": "User",
    "password": "MyPassword123",
    "phoneNumber": "099-999-9999"
  }
  ```
- ✅ ควร register สำเร็จ
- ✅ ลอง login ด้วย email + password ที่สร้างใหม่

## ผลลัพธ์
- ✅ Password ถูก hash ด้วย PBKDF2 + salt (100,000 iterations)
- ✅ เบอร์โทรถูก encrypt ด้วย AES
- ✅ Login ทำงานได้ถูกต้อง
- ✅ Register user ใหม่ทำงานได้ถูกต้อง
- ✅ ระบบปลอดภัยตามมาตรฐาน

## หมายเหตุ
- Password ทั้งหมดใน seed data ใช้: `Password123`
- ต้องลบ database เดิมก่อนเพื่อให้ seed data ใหม่ทำงาน
- ถ้าไม่ลบ database จะไม่ seed ใหม่ (มี check `if (await context.Users.AnyAsync())`)
