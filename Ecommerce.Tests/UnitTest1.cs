using Data;
using Ecommerce.Tests.Helpers;

namespace Ecommerce.Tests;

public class UnitTest1
{
    [Fact]
    public void InMemoryDatabase_ShouldCreateContext_Successfully()
    {
        AppDbContext context = TestDbContextFactory.CreateFresh();

        Assert.NotNull(context);
        Assert.NotNull(context.Users);
        Assert.NotNull(context.Products);
        Assert.NotNull(context.Orders);
        Assert.NotNull(context.OrderItems);

        context.Dispose();
    }
}
