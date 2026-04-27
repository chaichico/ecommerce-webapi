using Services.Interfaces;

namespace Ecommerce.Tests.Fakes;

public class FakeEncryptionService : IEncryptionService
{
    public string Encrypt(string plainText) => $"encrypted:{plainText}";

    public string Decrypt(string cipherText) =>
        cipherText.StartsWith("encrypted:") ? cipherText["encrypted:".Length..] : cipherText;
}
