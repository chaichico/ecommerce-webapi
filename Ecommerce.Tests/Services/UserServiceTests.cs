using Data;
using Ecommerce.Tests.Fakes;
using Ecommerce.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Models;
using Models.Dtos;
using Repositories;
using Services;

namespace Ecommerce.Tests.Services;

public class UserServiceTests
{
    private static IConfiguration BuildConfiguration()
    {
        Dictionary<string, string?> configValues = new Dictionary<string, string?>
        {
            { "Jwt:Key", "super-secret-key-for-testing-only-32chars!!" },
            { "Jwt:Issuer", "test-issuer" },
            { "Jwt:Audience", "test-audience" },
            { "Jwt:ExpiryInMinutes", "60" }
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsUserResponseDto()
    {
        AppDbContext context = TestDbContextFactory.CreateFresh();
        UserRepository userRepository = new UserRepository(context);
        FakePasswordHasher passwordHasher = new FakePasswordHasher();
        FakeEncryptionService encryptionService = new FakeEncryptionService();
        IConfiguration configuration = BuildConfiguration();

        UserService service = new UserService(userRepository, passwordHasher, encryptionService, configuration);

        RegisterUserDto dto = new RegisterUserDto
        {
            Email = "register@example.com",
            FirstName = "John",
            LastName = "Doe",
            Password = "password123",
            ConfirmPassword = "password123",
            PhoneNumber = "0812345678"
        };

        UserResponseDto result = await service.RegisterAsync(dto);

        Assert.Equal("register@example.com", result.Email);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        context.Dispose();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsException()
    {
        AppDbContext context = TestDbContextFactory.CreateFresh();
        await TestDataSeeder.CreateUserAsync(context, "duplicate@example.com");

        UserRepository userRepository = new UserRepository(context);
        FakePasswordHasher passwordHasher = new FakePasswordHasher();
        FakeEncryptionService encryptionService = new FakeEncryptionService();
        IConfiguration configuration = BuildConfiguration();

        UserService service = new UserService(userRepository, passwordHasher, encryptionService, configuration);

        RegisterUserDto dto = new RegisterUserDto
        {
            Email = "duplicate@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        await Assert.ThrowsAsync<Exception>(() => service.RegisterAsync(dto));
        context.Dispose();
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUser()
    {
        AppDbContext context = TestDbContextFactory.CreateFresh();
        FakePasswordHasher passwordHasher = new FakePasswordHasher();

        User user = new User
        {
            Email = "login@example.com",
            FirstName = "Login",
            LastName = "User",
            PasswordHash = passwordHasher.HashPassword("secret")
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        UserRepository userRepository = new UserRepository(context);
        FakeEncryptionService encryptionService = new FakeEncryptionService();
        IConfiguration configuration = BuildConfiguration();

        UserService service = new UserService(userRepository, passwordHasher, encryptionService, configuration);

        LoginDto dto = new LoginDto { Email = "login@example.com", Password = "secret" };
        LoginResponseDto result = await service.LoginAsync(dto);

        Assert.NotEmpty(result.Token);
        Assert.Equal("login@example.com", result.User.Email);
        context.Dispose();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        AppDbContext context = TestDbContextFactory.CreateFresh();
        FakePasswordHasher passwordHasher = new FakePasswordHasher();

        User user = new User
        {
            Email = "login2@example.com",
            FirstName = "Login",
            LastName = "User",
            PasswordHash = passwordHasher.HashPassword("correct")
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        UserRepository userRepository = new UserRepository(context);
        FakeEncryptionService encryptionService = new FakeEncryptionService();
        IConfiguration configuration = BuildConfiguration();

        UserService service = new UserService(userRepository, passwordHasher, encryptionService, configuration);

        LoginDto dto = new LoginDto { Email = "login2@example.com", Password = "wrong" };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(dto));
        context.Dispose();
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
    {
        AppDbContext context = TestDbContextFactory.CreateFresh();
        UserRepository userRepository = new UserRepository(context);
        FakePasswordHasher passwordHasher = new FakePasswordHasher();
        FakeEncryptionService encryptionService = new FakeEncryptionService();
        IConfiguration configuration = BuildConfiguration();

        UserService service = new UserService(userRepository, passwordHasher, encryptionService, configuration);

        LoginDto dto = new LoginDto { Email = "ghost@example.com", Password = "pass" };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(dto));
        context.Dispose();
    }
}
