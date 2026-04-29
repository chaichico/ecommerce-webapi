using Data;
using Ecommerce.Tests.Fakes;
using Ecommerce.Tests.Helpers;
using Models;

namespace Ecommerce.Tests.Services;

public class DbSeederTests
{
    [Fact]
    public async Task SeedAsync_EncryptsSeededPhoneNumbers()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        FakePasswordHasher passwordHasher = new FakePasswordHasher();
        FakeEncryptionService encryptionService = new FakeEncryptionService();

        List<User> users = await TestDataSeeder.SeedDefaultUsersAsync(context, passwordHasher, encryptionService);

        Assert.Equal(3, users.Count);
        Assert.All(users, user =>
        {
            Assert.NotNull(user.PhoneNumber);
            Assert.StartsWith("encrypted:", user.PhoneNumber);
        });
    }
}