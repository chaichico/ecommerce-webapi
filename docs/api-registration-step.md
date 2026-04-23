# User Registration API - Step by Step Guide

คู่มือการเขียนโค้ด User Registration API แบบละเอียด

---

## 🎯 ลำดับการเขียนโค้ด

### 1️⃣ **Models/DTOs/RegisterUserDto.cs** ← เริ่มที่นี่ก่อน

```csharp
using System.ComponentModel.DataAnnotations;

namespace Models.DTOs;

public class RegisterUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    // Optional
    public string? PhoneNumber { get; set; }
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

**จุดสำคัญ:**
- ใช้ `[Required]` สำหรับ field บังคับ
- ใช้ `[EmailAddress]` validate email format
- ใช้ `[Compare]` เช็คว่า Password ตรงกับ ConfirmPassword
- PhoneNumber เป็น optional ใช้ `?`

---

### 2️⃣ **Models/DTOs/UserResponseDto.cs**

```csharp
namespace Models.DTOs;

public class UserResponseDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    
    // ไม่มี Password, PasswordHash, PhoneNumber
}
```

**จุดสำคัญ:**
- ส่งแค่ข้อมูลที่ปลอดภัย
- ไม่มี validation attributes (เพราะเป็น response)

---

### 3️⃣ **Interfaces/IPasswordHasher.cs**

```csharp
namespace Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);
}
```

**จุดสำคัญ:**
- `HashPassword` - แปลง plain text เป็น hash
- `VerifyPassword` - เช็คว่า password ตรงกับ hash ไหม (ใช้ตอน login)

---

### 4️⃣ **Interfaces/IEncryptionService.cs**

```csharp
namespace Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
}
```

**จุดสำคัญ:**
- ใช้สำหรับ encrypt/decrypt เบอร์โทร
- Symmetric encryption (เข้ารหัส-ถอดรหัสได้)

---

### 5️⃣ **Interfaces/IUserRepository.cs**

```csharp
using Models;

namespace Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
}
```

**จุดสำคัญ:**
- `GetByEmailAsync` - หา user จาก email (ใช้ตอน login)
- `CreateAsync` - สร้าง user ใหม่
- `EmailExistsAsync` - เช็คว่า email ซ้ำไหม

---

### 6️⃣ **Interfaces/IUserService.cs**

```csharp
using Models;
using Models.DTOs;

namespace Interfaces;

public interface IUserService
{
    Task<UserResponseDto> RegisterAsync(RegisterUserDto dto);
}
```

**จุดสำคัญ:**
- รับ `RegisterUserDto` เข้ามา
- return `UserResponseDto` กลับไป

---

### 7️⃣ **Services/PasswordHasher.cs**

```csharp
using Interfaces;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        // ใช้ PBKDF2 algorithm
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
        
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));
        
        // เก็บทั้ง salt และ hash รวมกัน
        return $"{Convert.ToBase64String(salt)}.{hashed}";
    }
    
    public bool VerifyPassword(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        var salt = Convert.FromBase64String(parts[0]);
        var hash = parts[1];
        
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));
        
        return hash == hashed;
    }
}
```

**จุดสำคัญ:**
- ใช้ PBKDF2 (มาตรฐาน .NET)
- เก็บ salt กับ hash รวมกัน format: `{salt}.{hash}`
- VerifyPassword ใช้ตอน login

---

### 8️⃣ **Services/EncryptionService.cs**

```csharp
using Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    
    public EncryptionService(IConfiguration configuration)
    {
        // อ่าน key จาก appsettings หรือ ENV
        var keyString = configuration["Encryption:Key"] ?? "YourSecretKey1234567890123456"; // 32 chars
        var ivString = configuration["Encryption:IV"] ?? "YourIV1234567890"; // 16 chars
        
        _key = Encoding.UTF8.GetBytes(keyString);
        _iv = Encoding.UTF8.GetBytes(ivString);
    }
    
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        
        return Convert.ToBase64String(ms.ToArray());
    }
    
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return encryptedText;
        
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(encryptedText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}
```

**จุดสำคัญ:**
- ใช้ AES (Symmetric encryption)
- ต้องมี Key และ IV (เก็บใน appsettings.json)
- **สำคัญ:** ต้องเพิ่ม Key/IV ใน appsettings.json ด้วย

---

### 9️⃣ **Repositories/UserRepository.cs**

```csharp
using Data;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
    
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email);
    }
}
```

**จุดสำคัญ:**
- ใช้ Entity Framework Core
- `FirstOrDefaultAsync` - หา 1 record (return null ถ้าไม่เจอ)
- `AnyAsync` - เช็คว่ามีไหม (return true/false)

---

### 🔟 **Services/UserService.cs**

```csharp
using Interfaces;
using Models;
using Models.DTOs;

namespace Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEncryptionService _encryptionService;
    
    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEncryptionService encryptionService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _encryptionService = encryptionService;
    }
    
    public async Task<UserResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        // 1. เช็คว่า email ซ้ำไหม
        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            throw new Exception("Email already exists");
        }
        
        // 2. Hash password
        var passwordHash = _passwordHasher.HashPassword(dto.Password);
        
        // 3. Encrypt phone (ถ้ามี)
        string? encryptedPhone = null;
        if (!string.IsNullOrEmpty(dto.PhoneNumber))
        {
            encryptedPhone = _encryptionService.Encrypt(dto.PhoneNumber);
        }
        
        // 4. สร้าง User entity
        var user = new User
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PasswordHash = passwordHash,
            PhoneNumber = encryptedPhone
        };
        
        // 5. บันทึกลง database
        await _userRepository.CreateAsync(user);
        
        // 6. Return DTO (ไม่ส่ง password ออกไป)
        return new UserResponseDto
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}
```

**จุดสำคัญ:**
- Inject dependencies ทั้งหมดที่ต้องใช้
- เช็ค email ซ้ำก่อน
- Hash password ก่อนเก็บ
- Encrypt phone (ถ้ามี)
- Return DTO ไม่ใช่ Entity

---

### 1️⃣1️⃣ **Controllers/UsersController.cs**

```csharp
using Interfaces;
using Microsoft.AspNetCore.Mvc;
using Models.DTOs;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        try
        {
            var result = await _userService.RegisterAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
```

**จุดสำคัญ:**
- Route: `POST /api/users/register`
- `[FromBody]` - รับ JSON จาก request body
- Try-catch จัดการ error
- Return `Ok(result)` ถ้าสำเร็จ, `BadRequest` ถ้าผิดพลาด

---

### 1️⃣2️⃣ **Program.cs** - Register Services

เพิ่มใน `Program.cs` (ก่อน `var app = builder.Build();`):

```csharp
// Register services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
```

**จุดสำคัญ:**
- ใส่ก่อน `var app = builder.Build();`
- ใช้ `AddScoped` สำหรับ services ที่ต้องการ per-request lifetime

---

### 1️⃣3️⃣ **appsettings.json** - เพิ่ม Encryption Keys

```json
{
  "Encryption": {
    "Key": "YourSecretKey1234567890123456",
    "IV": "YourIV1234567890"
  }
}
```

**จุดสำคัญ:**
- Key ต้องยาว 32 characters
- IV ต้องยาว 16 characters
- **Production:** ใช้ Environment Variables แทน

---

## ✅ Checklist

- [x] 1. RegisterUserDto
- [x] 2. UserResponseDto
- [x] 3. IPasswordHasher
- [x] 4. IEncryptionService
- [x] 5. IUserRepository
- [x] 6. IUserService
- [x] 7. PasswordHasher
- [x] 8. EncryptionService
- [x] 9. UserRepository
- [x] 10. UserService
- [x] 11. UsersController
- [x] 12. Program.cs (DI)
- [x] 13. appsettings.json

---

## 🧪 ทดสอบ API

### Request
```http
POST http://localhost:8080/api/users/register
Content-Type: application/json

{
  "email": "test@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "0812345678",
  "password": "password123",
  "confirmPassword": "password123"
}
```

### Response (Success)
```json
{
  "email": "test@example.com",
  "firstName": "John",
  "lastName": "Doe"
}
```

### Response (Error - Email exists)
```json
{
  "message": "Email already exists"
}
```

---

## 📝 หมายเหตุ

1. **Password Hashing**: ใช้ PBKDF2 (มาตรฐาน .NET)
2. **Phone Encryption**: ใช้ AES Symmetric Encryption
3. **Validation**: ใช้ Data Annotations ใน DTO
4. **Error Handling**: ใช้ try-catch ใน Controller
5. **Dependency Injection**: Register ทุก service ใน Program.cs

---

## 🔐 Security Best Practices

- ✅ ไม่เก็บ plain text password
- ✅ ใช้ salt แยกสำหรับแต่ละ password
- ✅ Encrypt ข้อมูล sensitive (เบอร์โทร)
- ✅ ไม่ส่ง password hash ออกไป API
- ✅ Validate input ด้วย Data Annotations
- ✅ เก็บ encryption key ใน configuration (ไม่ hard-code)

---

## 🚀 Next Steps

หลังจากทำ User Registration เสร็จแล้ว:
1. User Login (JWT Token)
2. Create Order
3. Update Order
4. Confirm Order
5. Admin APIs
