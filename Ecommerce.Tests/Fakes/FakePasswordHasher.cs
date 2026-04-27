using Services.Interfaces;

namespace Ecommerce.Tests.Fakes;

public class FakePasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => $"hashed:{password}";

    public bool VerifyPassword(string password, string passwordHash) =>
        passwordHash == $"hashed:{password}";
}
