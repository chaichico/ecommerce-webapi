namespace Ecommerce.Tests.Services;

using global::Services;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

public class EncryptionServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────
    private static EncryptionService BuildService(string? key = "1234567890123456") // 16 bytes
    {
        var configData = new Dictionary<string, string?>();
        if (key != null)
        {
            configData["Encryption:Key"] = key;
        }
        
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        return new EncryptionService(config);
    }

    // ── Constructor tests ─────────────────────────────────────────────────
    
    [Fact]
    public void Constructor_With16ByteKey_DoesNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() => BuildService("1234567890123456")); // 16 ASCII chars = 16 bytes
        
        // Assert
        Assert.Null(exception);
    }
    
    [Fact]
    public void Constructor_With24ByteKey_DoesNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() => BuildService("123456789012345678901234")); // 24 ASCII chars = 24 bytes
        
        // Assert
        Assert.Null(exception);
    }
    
    [Fact]
    public void Constructor_With32ByteKey_DoesNotThrow()
    {
        // Arrange & Act
        var exception = Record.Exception(() => BuildService("12345678901234567890123456789012")); // 32 ASCII chars = 32 bytes
        
        // Assert
        Assert.Null(exception);
    }
    
    [Fact]
    public void Constructor_MissingKey_ThrowsInvalidOperationException()
    {
        // Arrange
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new EncryptionService(config));
        Assert.Contains("Encryption:Key is not configured", exception.Message);
    }
    
    [Fact]
    public void Constructor_InvalidKeyLength_ThrowsInvalidOperationException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => BuildService("1234567890")); // 10 bytes - invalid
        Assert.Contains("must be 16, 24, or 32 bytes", exception.Message);
    }

    // ── Encrypt tests ─────────────────────────────────────────────────────
    
    [Fact]
    public void Encrypt_NullInput_ReturnsNull()
    {
        // Arrange
        var service = BuildService();
        
        // Act
        var result = service.Encrypt(null!);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void Encrypt_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var service = BuildService();
        
        // Act
        var result = service.Encrypt("");
        
        // Assert
        Assert.Equal("", result);
    }
    
    [Fact]
    public void Encrypt_ValidText_ReturnsBase64String()
    {
        // Arrange
        var service = BuildService();
        var plainText = "Hello World";
        
        // Act
        var result = service.Encrypt(plainText);
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(plainText, result); // Should not return plaintext
        
        // Verify it's valid Base64
        var exception = Record.Exception(() => Convert.FromBase64String(result));
        Assert.Null(exception);
    }

    // ── Decrypt tests ─────────────────────────────────────────────────────
    
    [Fact]
    public void Decrypt_NullInput_ReturnsNull()
    {
        // Arrange
        var service = BuildService();
        
        // Act
        var result = service.Decrypt(null!);
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void Decrypt_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var service = BuildService();
        
        // Act
        var result = service.Decrypt("");
        
        // Assert
        Assert.Equal("", result);
    }
    
    [Fact]
    public void Decrypt_TooShortPayload_ThrowsCryptographicException()
    {
        // Arrange
        var service = BuildService();
        // Create a Base64 string from a byte array with 16 bytes or less
        var shortPayload = Convert.ToBase64String(new byte[16]);
        
        // Act & Assert
        var exception = Assert.Throws<CryptographicException>(() => service.Decrypt(shortPayload));
        Assert.Contains("Invalid encrypted payload", exception.Message);
    }

    // ── Round-trip tests ──────────────────────────────────────────────────
    
    [Fact]
    public void EncryptThenDecrypt_ReturnsOriginalText()
    {
        // Arrange
        var service = BuildService();
        var originalText = "This is a secret message!";
        
        // Act
        var encrypted = service.Encrypt(originalText);
        var decrypted = service.Decrypt(encrypted);
        
        // Assert
        Assert.Equal(originalText, decrypted);
    }
    
    [Fact]
    public void EncryptThenDecrypt_ProduceDifferentCipherEachTime()
    {
        // Arrange
        var service = BuildService();
        var plainText = "Same input";
        
        // Act
        var encrypted1 = service.Encrypt(plainText);
        var encrypted2 = service.Encrypt(plainText);
        
        // Assert
        Assert.NotEqual(encrypted1, encrypted2); // Different IV should produce different ciphertexts
        
        // But both should decrypt to the same plaintext
        var decrypted1 = service.Decrypt(encrypted1);
        var decrypted2 = service.Decrypt(encrypted2);
        Assert.Equal(plainText, decrypted1);
        Assert.Equal(plainText, decrypted2);
    }
}
