using Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create(string databaseName)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        AppDbContext context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static AppDbContext CreateFresh()
    {
        return Create(Guid.NewGuid().ToString());
    }
}
