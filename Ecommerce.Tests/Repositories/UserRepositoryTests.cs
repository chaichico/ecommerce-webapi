using Data;
using Ecommerce.Tests.Helpers;
using Models.Entities;
using Repositories;

namespace Ecommerce.Tests.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task GetByEmail_WhenUserExists_ReturnsUser()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User created = await TestDataSeeder.CreateUserAsync(context, "find@example.com");

        UserRepository repository = new UserRepository(context);
        User? result = await repository.GetByEmail("find@example.com");

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("find@example.com", result.Email);
    }

    [Fact]
    public async Task GetByEmail_WhenUserNotExists_ReturnsNull()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();

        UserRepository repository = new UserRepository(context);
        User? result = await repository.GetByEmail("nobody@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task Create_ShouldPersistUser()
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

        await repository.Create(user);

        Assert.True(user.Id > 0);
        Assert.Equal("new@example.com", user.Email);
    }

    [Fact]
    public async Task EmailExists_WhenEmailExists_ReturnsTrue()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        await TestDataSeeder.CreateUserAsync(context, "exists@example.com");

        UserRepository repository = new UserRepository(context);
        bool result = await repository.EmailExists("exists@example.com");

        Assert.True(result);
    }

    [Fact]
    public async Task EmailExists_WhenEmailNotExists_ReturnsFalse()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();

        UserRepository repository = new UserRepository(context);
        bool result = await repository.EmailExists("notexists@example.com");

        Assert.False(result);
    }
}
