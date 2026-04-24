using Services.Interfaces;
using Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Models.Dtos;
using Repositories.Interfaces;
namespace Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;     // อ่านค่า JWT config

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, IEncryptionService encryptionService, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _encryptionService = encryptionService;
        _configuration = configuration;
    }

    public async Task<UserResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        // เช็คว่า email ซ้ำไหม
        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            throw new Exception("Email already exists");
        }

        // เรียก Hash password
        string passwordHash = _passwordHasher.HashPassword(dto.Password);

        // encrypt phone ถ้ามี
        string? encryptedPhone = null;
        if (!string.IsNullOrEmpty(dto.PhoneNumber))
        {
            encryptedPhone = _encryptionService.Encrypt(dto.PhoneNumber);
        }

        // สร้าง User entity
        User user = new User
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PasswordHash = passwordHash,
            PhoneNumber = encryptedPhone
        };

        // บันทึกลง database
        await _userRepository.CreateAsync(user);

        // Return DTO (No password)
        return new UserResponseDto
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };

    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        //  หา user จาก email
        User user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }
        
        // ตรวจสอบ password
        if (!_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // สร้าง JWT Token
        string token = GenerateJwtToken(user);

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