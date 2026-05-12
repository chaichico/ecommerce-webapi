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
}
