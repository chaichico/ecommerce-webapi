using Data;
using Ecommerce.Tests.Helpers;
using Models.Entities;
using Repositories;

namespace Ecommerce.Tests.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ReturnsUser()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User created = await TestDataSeeder.CreateUserAsync(context, "find@example.com");

        UserRepository repository = new UserRepository(context);
        User? result = await repository.GetByEmailAsync("find@example.com");

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("find@example.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserNotExists_ReturnsNull()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();

        UserRepository repository = new UserRepository(context);
        User? result = await repository.GetByEmailAsync("nobody@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistUser()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        UserRepository repository = new UserRepository(context);

        User user = new User
        {
            Email = "new@example.com",
            FirstName = "New",
            LastName = "User",
            PasswordHash = "hash123"
        };

        User created = await repository.CreateAsync(user);

        Assert.True(created.Id > 0);
        Assert.Equal("new@example.com", created.Email);
    }

    [Fact]
    public async Task EmailExistsAsync_WhenEmailExists_ReturnsTrue()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        await TestDataSeeder.CreateUserAsync(context, "exists@example.com");

        UserRepository repository = new UserRepository(context);
        bool result = await repository.EmailExistsAsync("exists@example.com");

        Assert.True(result);
    }

    [Fact]
    public async Task EmailExistsAsync_WhenEmailNotExists_ReturnsFalse()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();

        UserRepository repository = new UserRepository(context);
        bool result = await repository.EmailExistsAsync("notexists@example.com");

        Assert.False(result);
    }
}
