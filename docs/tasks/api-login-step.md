# User Login API - Step by Step Guide

คู่มือการเขียนโค้ด User Login API แบบละเอียด

---

## 🎯 ลำดับการเขียนโค้ด

### 1️⃣ **Models/DTOs/LoginDto.cs** ← เริ่มที่นี่ก่อน

```csharp
using System.ComponentModel.DataAnnotations;

namespace Models.DTOs;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}
```

**จุดสำคัญ:**
- รับแค่ Email และ Password
- ใช้ `[Required]` สำหรับ field บังคับ
- ใช้ `[EmailAddress]` validate email format

---

### 2️⃣ **Models/DTOs/LoginResponseDto.cs**

```csharp
namespace Models.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserResponseDto User { get; set; } = null!;
}
```

**จุดสำคัญ:**
- Return JWT Token
- Return User info (Email, FirstName, LastName)
- ใช้ `UserResponseDto` ที่มีอยู่แล้ว

---

### 3️⃣ **appsettings.json** - เพิ่ม JWT Settings

```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyForJWT1234567890",
    "Issuer": "EcommerceAPI",
    "Audience": "EcommerceClient",
    "ExpiryInMinutes": 60
  },
  "Encryption": {
    "Key": "YourSecretKey1234567890123456",
    "IV": "YourIV1234567890"
  }
}
```

**จุดสำคัญ:**
- `Key` - ต้องยาวพอ (อย่างน้อย 32 characters)
- `Issuer` - ชื่อ API ของเรา
- `Audience` - ใครจะใช้ token นี้
- `ExpiryInMinutes` - token หมดอายุเมื่อไหร่ (60 นาที)

---

### 4️⃣ **Interfaces/IUserService.cs** - เพิ่ม LoginAsync

```csharp
using Models;
using Models.DTOs;

namespace Interfaces;

public interface IUserService
{
    Task<UserResponseDto> RegisterAsync(RegisterUserDto dto);
    Task<LoginResponseDto> LoginAsync(LoginDto dto); // ← เพิ่มบรรทัดนี้
}
```

**จุดสำคัญ:**
- เพิ่ม method `LoginAsync` ใน interface ที่มีอยู่
- รับ `LoginDto` เข้ามา
- return `LoginResponseDto` กลับไป

---

### 5️⃣ **Services/UserService.cs** - Implement LoginAsync

```csharp
using Interfaces;
using Models;
using Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    
    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEncryptionService encryptionService,
        IConfiguration configuration) // ← เพิ่ม IConfiguration
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _encryptionService = encryptionService;
        _configuration = configuration;
    }
    
    public async Task<UserResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        // ... existing code ...
    }
    
    // ← เพิ่ม method นี้
    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        // 1. หา user จาก email
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }
        
        // 2. ตรวจสอบ password
        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }
        
        // 3. สร้าง JWT Token
        var token = GenerateJwtToken(user);
        
        // 4. Return response
        return new LoginResponseDto
        {
            Token = token,
            User = new UserResponseDto
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        };
    }
    
    private string GenerateJwtToken(User user)
    {
        // อ่านค่า config จาก appsettings.json
        var jwtKey = _configuration["Jwt:Key"] ?? throw new Exception("JWT Key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new Exception("JWT Issuer not configured");
        var jwtAudience = _configuration["Jwt:Audience"] ?? throw new Exception("JWT Audience not configured");
        var jwtExpiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");
        
        // สร้าง claims (ข้อมูลที่จะเก็บใน token)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        // สร้าง signing key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        // สร้าง token
        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtExpiryMinutes),
            signingCredentials: credentials
        );
        
        // แปลง token เป็น string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**จุดสำคัญ:**
- เพิ่ม `IConfiguration` ใน constructor เพื่ออ่านค่า JWT config
- `LoginAsync` - ตรวจสอบ email และ password
- `GenerateJwtToken` - สร้าง JWT token พร้อม claims (Email, FirstName, LastName)
- ใช้ `UnauthorizedAccessException` สำหรับ login ผิดพลาด
- Token หมดอายุตาม config (default 60 นาที)

---

### 6️⃣ **Controllers/UsersController.cs** - เพิ่ม Login endpoint

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
        // ... existing code ...
    }
    
    // ← เพิ่ม endpoint นี้
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _userService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
```

**จุดสำคัญ:**
- Route: `POST /api/users/login`
- `[FromBody]` - รับ JSON จาก request body
- Return `Unauthorized` (401) ถ้า login ผิด
- Return `Ok` (200) พร้อม token ถ้าสำเร็จ

---

### 7️⃣ **Program.cs** - เพิ่ม JWT Authentication

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

// ← เพิ่ม JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("JWT Key not configured");
        var jwtIssuer = builder.Configuration["Jwt:Issuer"];
        var jwtAudience = builder.Configuration["Jwt:Audience"];
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ← เพิ่ม middleware (ต้องอยู่ก่อน MapControllers)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

**จุดสำคัญ:**
- เพิ่ม `AddAuthentication` และ `AddJwtBearer`
- ตั้งค่า `TokenValidationParameters` ให้ตรงกับตอนสร้าง token
- เพิ่ม `app.UseAuthentication()` และ `app.UseAuthorization()` ก่อน `MapControllers()`
- **ลำดับสำคัญ:** Authentication → Authorization → MapControllers

---

### 8️⃣ **ecommerce.csproj** - เพิ่ม NuGet Packages

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.0" />
  </ItemGroup>

</Project>
```

**จุดสำคัญ:**
- ต้องมี `Microsoft.AspNetCore.Authentication.JwtBearer`
- ต้องมี `System.IdentityModel.Tokens.Jwt`
- ถ้ายังไม่มี ให้รันคำสั่ง:
```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
```

---

## ✅ Checklist

- [x] 1. LoginDto
- [x] 2. LoginResponseDto
- [x] 3. appsettings.json (JWT config)
- [x] 4. IUserService (เพิ่ม LoginAsync)
- [x] 5. UserService (Implement LoginAsync + GenerateJwtToken)
- [x] 6. UsersController (เพิ่ม Login endpoint)
- [x] 7. Program.cs (JWT Authentication)
- [x] 8. Install NuGet packages

---

## 🧪 ทดสอบ API

### 1. Register User ก่อน
```http
POST http://localhost:8080/api/users/register
Content-Type: application/json

{
  "email": "test@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "password": "password123",
  "confirmPassword": "password123"
}
```

### 2. Login
```http
POST http://localhost:8080/api/users/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "password123"
}
```

### Response (Success)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "email": "test@example.com",
    "firstName": "John",
    "lastName": "Doe"
  }
}
```

### Response (Error - Invalid credentials)
```json
{
  "message": "Invalid email or password"
}
```

### 3. ทดสอบใช้ Token (ตัวอย่าง - จะทำใน API ถัดไป)
```http
GET http://localhost:8080/api/orders
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 🔍 JWT Token Payload

Token ที่สร้างจะมีข้อมูลดังนี้:

```json
{
  "sub": "test@example.com",
  "email": "test@example.com",
  "given_name": "John",
  "family_name": "Doe",
  "jti": "unique-token-id",
  "exp": 1234567890,
  "iss": "EcommerceAPI",
  "aud": "EcommerceClient"
}
```

**Claims ที่เก็บ:**
- `sub` (Subject) - Email ของ user
- `email` - Email
- `given_name` - FirstName
- `family_name` - LastName
- `jti` (JWT ID) - Unique ID ของ token
- `exp` (Expiry) - เวลาหมดอายุ
- `iss` (Issuer) - ผู้ออก token
- `aud` (Audience) - ผู้ใช้ token

---

## 📝 หมายเหตุ

1. **Password Verification**: ใช้ `VerifyPassword` ที่เขียนไว้แล้วใน `PasswordHasher`
2. **JWT Token**: ใช้ `System.IdentityModel.Tokens.Jwt` library
3. **Token Expiry**: Default 60 นาที (ตั้งค่าได้ใน appsettings.json)
4. **Error Message**: ใช้ message เดียวกันสำหรับ email/password ผิด (security best practice)
5. **Claims**: เก็บข้อมูล user ใน token เพื่อไม่ต้อง query database ทุกครั้ง

---

## 🔐 Security Best Practices

- ✅ ใช้ message เดียวกันสำหรับ "email ไม่มี" และ "password ผิด" (ป้องกัน user enumeration)
- ✅ ใช้ `UnauthorizedAccessException` สำหรับ login ผิดพลาด
- ✅ เก็บ JWT Key ใน configuration (ไม่ hard-code)
- ✅ ตั้งค่า token expiry (ไม่ให้ token ใช้ได้ตลอดไป)
- ✅ Validate Issuer, Audience, Lifetime, และ Signing Key
- ✅ ใช้ HTTPS ใน production (ป้องกัน token ถูกขโมย)

---

## 🎯 วิธีใช้ JWT Token ใน API อื่น

เมื่อต้องการป้องกัน API ให้ใช้ได้เฉพาะ user ที่ login แล้ว:

```csharp
[Authorize] // ← เพิ่ม attribute นี้
[HttpPost]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
{
    // ดึง email จาก token
    var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
    
    // ใช้ userEmail ในการสร้าง order
    // ...
}
```

**จุดสำคัญ:**
- เพิ่ม `[Authorize]` attribute
- ดึงข้อมูล user จาก `User.FindFirst(ClaimTypes.Email)`
- ไม่ต้อง query database เพื่อหา user (ข้อมูลอยู่ใน token แล้ว)

---

## 🚀 Next Steps

หลังจากทำ User Login เสร็จแล้ว:
1. ✅ User Registration
2. ✅ User Login (JWT Token)
3. Create Order (ใช้ JWT Authentication)
4. Update Order (ใช้ JWT Authentication)
5. Confirm Order (ใช้ JWT Authentication)
6. Admin APIs (ใช้ Basic Authentication)

---

## 🐛 Troubleshooting

### ปัญหา: "JWT Key not configured"
**แก้ไข:** เพิ่ม JWT config ใน appsettings.json

### ปัญหา: Token ไม่ work (401 Unauthorized)
**เช็ค:**
1. ใส่ `app.UseAuthentication()` ก่อน `app.UseAuthorization()` ใน Program.cs หรือยัง
2. JWT Key ใน appsettings ตรงกับที่ใช้สร้าง token หรือไม่
3. Token หมดอายุหรือยัง
4. ส่ง token ใน header ถูกต้องหรือไม่: `Authorization: Bearer {token}`

### ปัญหา: "Invalid email or password" แม้ว่า password ถูก
**เช็ค:**
1. User ถูกสร้างด้วย `PasswordHasher.HashPassword` หรือไม่
2. Format ของ password hash ใน database ถูกต้องหรือไม่ (ต้องมี format: `{salt}.{hash}`)

---

## 📚 เอกสารเพิ่มเติม

- [JWT.io](https://jwt.io/) - ทดสอบ decode JWT token
- [Microsoft JWT Authentication Docs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [RFC 7519 - JWT Specification](https://datatracker.ietf.org/doc/html/rfc7519)
