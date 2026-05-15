using AutoMapper;
using Microsoft.Extensions.Configuration;
using Models.Dtos.Requests;
using Models.Dtos.Responses;
using Models.Entities;
using Moq;
using Repositories.Interfaces;
using Services;
using Services.Interfaces;

namespace Ecommerce.Tests.Services;

public class UserServiceTests
{
    // ── Shared mock fields ──────────────────────────────────────────────
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IEncryptionService> _encryptionMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly IConfiguration _configuration;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        Dictionary<string, string?> configValues = new Dictionary<string, string?>
        {
            { "Jwt:Key", "super-secret-key-for-testing-only-32chars!!" },
            { "Jwt:Issuer", "test-issuer" },
            { "Jwt:Audience", "test-audience" },
            { "Jwt:ExpiryInMinutes", "60" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _sut = new UserService(
            _userRepoMock.Object,
            _passwordHasherMock.Object,
            _encryptionMock.Object,
            _configuration,
            _mapperMock.Object);
    }

    // ── RegisterAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.EmailExists("dup@example.com"))
            .ReturnsAsync(true);

        RegisterUserDto dto = new RegisterUserDto
        {
            Email = "dup@example.com",
            FirstName = "John",
            LastName = "Doe",
            Password = "pass123",
            ConfirmPassword = "pass123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(dto));
    }

    [Fact]
    public async Task RegisterAsync_WithPhoneNumber_CallsEncryptOnce()
    {
        // Arrange
        _userRepoMock.Setup(r => r.EmailExists(It.IsAny<string>())).ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed");
        _encryptionMock.Setup(e => e.Encrypt("0812345678")).Returns("encrypted_phone");
        _userRepoMock.Setup(r => r.Create(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(m => m.Map<UserResponseDto>(It.IsAny<User>())).Returns(new UserResponseDto());

        RegisterUserDto dto = new RegisterUserDto
        {
            Email = "new@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            Password = "pass123",
            ConfirmPassword = "pass123",
            PhoneNumber = "0812345678"
        };

        // Act
        await _sut.RegisterAsync(dto);

        // Assert
        _encryptionMock.Verify(e => e.Encrypt("0812345678"), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithoutPhoneNumber_DoesNotCallEncrypt()
    {
        // Arrange
        _userRepoMock.Setup(r => r.EmailExists(It.IsAny<string>())).ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed");
        _userRepoMock.Setup(r => r.Create(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(m => m.Map<UserResponseDto>(It.IsAny<User>())).Returns(new UserResponseDto());

        RegisterUserDto dto = new RegisterUserDto
        {
            Email = "new@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            Password = "pass123",
            ConfirmPassword = "pass123",
            PhoneNumber = null
        };

        // Act
        await _sut.RegisterAsync(dto);

        // Assert
        _encryptionMock.Verify(e => e.Encrypt(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ValidData_CallsHashPasswordOnce()
    {
        // Arrange
        _userRepoMock.Setup(r => r.EmailExists(It.IsAny<string>())).ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.HashPassword("pass123")).Returns("hashed_pass");
        _userRepoMock.Setup(r => r.Create(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(m => m.Map<UserResponseDto>(It.IsAny<User>())).Returns(new UserResponseDto());

        RegisterUserDto dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            FirstName = "John",
            LastName = "Doe",
            Password = "pass123",
            ConfirmPassword = "pass123"
        };

        // Act
        await _sut.RegisterAsync(dto);

        // Assert
        _passwordHasherMock.Verify(h => h.HashPassword("pass123"), Times.Once);
    }

    // ── LoginAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_EmailNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetByEmail("nobody@example.com"))
            .ReturnsAsync((User?)null);

        LoginDto dto = new LoginDto { Email = "nobody@example.com", Password = "anypass" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com", PasswordHash = "correct_hash" };
        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.VerifyPassword("wrongpass", "correct_hash"))
            .Returns(false);

        LoginDto dto = new LoginDto { Email = "user@example.com", Password = "wrongpass" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponseDtoWithToken()
    {
        // Arrange
        User user = new User
        {
            Id = 1,
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "correct_hash"
        };
        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correctpass", "correct_hash"))
            .Returns(true);
        _mapperMock
            .Setup(m => m.Map<UserResponseDto>(user))
            .Returns(new UserResponseDto { Email = "user@example.com" });

        LoginDto dto = new LoginDto { Email = "user@example.com", Password = "correctpass" };

        // Act
        LoginResponseDto result = await _sut.LoginAsync(dto);

        // Assert
        Assert.NotEmpty(result.Token);
    }

    [Fact]
    public async Task LoginAsync_MissingJwtKey_ThrowsInvalidOperationException()
    {
        // Arrange
        UserService sut = BuildSutWithConfig(new Dictionary<string, string?>
        {
            // "Jwt:Key" ถูกตัดออก
            { "Jwt:Issuer",          "test-issuer" },
            { "Jwt:Audience",        "test-audience" },
            { "Jwt:ExpiryInMinutes", "60" }
        });

        SetupValidLoginMocks();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.LoginAsync(new LoginDto { Email = "user@example.com", Password = "correctpass" }));
    }

    [Fact]
    public async Task LoginAsync_MissingJwtIssuer_ThrowsInvalidOperationException()
    {
        // Arrange
        UserService sut = BuildSutWithConfig(new Dictionary<string, string?>
        {
            { "Jwt:Key",             "super-secret-key-for-testing-only-32chars!!" },
            // "Jwt:Issuer" ถูกตัดออก
            { "Jwt:Audience",        "test-audience" },
            { "Jwt:ExpiryInMinutes", "60" }
        });

        SetupValidLoginMocks();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.LoginAsync(new LoginDto { Email = "user@example.com", Password = "correctpass" }));
    }

    [Fact]
    public async Task LoginAsync_MissingJwtAudience_ThrowsInvalidOperationException()
    {
        // Arrange
        UserService sut = BuildSutWithConfig(new Dictionary<string, string?>
        {
            { "Jwt:Key",             "super-secret-key-for-testing-only-32chars!!" },
            { "Jwt:Issuer",          "test-issuer" },
            // "Jwt:Audience" ถูกตัดออก
            { "Jwt:ExpiryInMinutes", "60" }
        });

        SetupValidLoginMocks();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.LoginAsync(new LoginDto { Email = "user@example.com", Password = "correctpass" }));
    }

    [Fact]
    public async Task LoginAsync_MissingExpiryInMinutes_UsesDefaultAndReturnsToken()
    {
        // Arrange
        UserService sut = BuildSutWithConfig(new Dictionary<string, string?>
        {
            { "Jwt:Key",     "super-secret-key-for-testing-only-32chars!!" },
            { "Jwt:Issuer",  "test-issuer" },
            { "Jwt:Audience","test-audience" }
            // "Jwt:ExpiryInMinutes" ถูกตัดออก → ควร fallback เป็น "60"
        });

        SetupValidLoginMocks();

        // Act
        LoginResponseDto result = await sut.LoginAsync(
            new LoginDto { Email = "user@example.com", Password = "correctpass" });

        // Assert
        Assert.NotEmpty(result.Token);
    }

    // ── Helper Methods ───────────────────────────────────────────────────

    private UserService BuildSutWithConfig(Dictionary<string, string?> overrides)
    {
        IConfiguration cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(overrides)
            .Build();

        return new UserService(
            _userRepoMock.Object,
            _passwordHasherMock.Object,
            _encryptionMock.Object,
            cfg,
            _mapperMock.Object);
    }

    private void SetupValidLoginMocks()
    {
        User user = new User
        {
            Id = 1,
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "correct_hash"
        };

        _userRepoMock
            .Setup(r => r.GetByEmail("user@example.com"))
            .ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correctpass", "correct_hash"))
            .Returns(true);
        _mapperMock
            .Setup(m => m.Map<UserResponseDto>(user))
            .Returns(new UserResponseDto { Email = "user@example.com" });
    }
}
