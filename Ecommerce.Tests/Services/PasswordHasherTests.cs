using Services;

namespace Ecommerce.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void HashPassword_ValidPassword_ReturnsNonEmptyString()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var result = _sut.HashPassword(password);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsFormattedHash()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var result = _sut.HashPassword(password);

        // Assert
        var parts = result.Split('.');
        Assert.Equal(2, parts.Length);
        Assert.NotEmpty(parts[0]); // salt
        Assert.NotEmpty(parts[1]); // hash
    }

    [Fact]
    public void HashPassword_CalledTwice_ReturnsDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var hash1 = _sut.HashPassword(password);
        var hash2 = _sut.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123";
        var hash = _sut.HashPassword(password);

        // Act
        var result = _sut.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123";
        var wrongPassword = "WrongPassword456";
        var hash = _sut.HashPassword(password);

        // Act
        var result = _sut.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ReturnsTrue()
    {
        // Arrange
        var password = "";
        var hash = _sut.HashPassword(password);

        // Act
        var result = _sut.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }
}
