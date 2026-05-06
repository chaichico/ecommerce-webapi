using Services.Interfaces;
using Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Models.Dtos.Requests;
using Models.Dtos.Responses;
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
        if (await _userRepository.EmailExists(dto.Email))
        {
            throw new InvalidOperationException("Email already exists");
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
        await _userRepository.Create(user);

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
        User? user = await _userRepository.GetByEmail(dto.Email);
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
        string jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        string jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        string jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
        int jwtExpiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");
        
        // สร้าง claims (ข้อมูลที่จะเก็บใน token)
        Claim[] claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        
        // สร้าง signing key
        SymmetricSecurityKey key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        // สร้าง token
        JwtSecurityToken token = new JwtSecurityToken(
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