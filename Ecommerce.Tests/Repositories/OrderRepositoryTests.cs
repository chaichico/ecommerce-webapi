using Data;
using Ecommerce.Tests.Helpers;
using Models.Entities;
using Models.Enums;
using Repositories;

namespace Ecommerce.Tests.Repositories;

public class OrderRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistOrderWithItems()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context);
        Product product = await TestDataSeeder.CreateProductAsync(context);

        Order order = new Order
        {
            OrderNumber = "ORD-TEST-001",
            Status = OrderStatus.Pending,
            ShippingAddress = string.Empty,
            UserId = user.Id,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    Quantity = 2,
                    UnitPrice = product.Price
                }
            },
            TotalPrice = product.Price * 2
        };

        OrderRepository repository = new OrderRepository(context);
        Order created = await repository.CreateAsync(order);

        Assert.True(created.Id > 0);
        Assert.Equal("ORD-TEST-001", created.OrderNumber);
        Assert.Single(created.Items);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenOrderExists_ReturnsOrderWithItems()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context);
        Product product = await TestDataSeeder.CreateProductAsync(context);
        Order created = await TestDataSeeder.CreateOrderWithItemsAsync(context, user.Id, product);

        OrderRepository repository = new OrderRepository(context);
        Order? result = await repository.GetByOrderIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.NotEmpty(result.Items);
        Assert.NotNull(result.User);
    }

    [Fact]
    public async Task GetByOrderIdAsync_WhenOrderNotExists_ReturnsNull()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();

        OrderRepository repository = new OrderRepository(context);
        Order? result = await repository.GetByOrderIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchOrdersAsync_ByOrderNumber_ReturnsMatchingOrders()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context);

        Order order = new Order
        {
            OrderNumber = "ORD-SEARCH-ABC",
            Status = OrderStatus.Pending,
            ShippingAddress = string.Empty,
            UserId = user.Id,
            TotalPrice = 0
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        OrderRepository repository = new OrderRepository(context);
        List<Order> results = await repository.SearchOrdersAsync("SEARCH", null, null);

        Assert.Single(results);
        Assert.Equal("ORD-SEARCH-ABC", results[0].OrderNumber);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context);
        Order order = await TestDataSeeder.CreateOrderAsync(context, user.Id);

        OrderRepository repository = new OrderRepository(context);
        Order? toUpdate = await repository.GetByOrderIdAsync(order.Id);
        Assert.NotNull(toUpdate);

        toUpdate.Status = OrderStatus.Confirmed;
        Order updated = await repository.UpdateAsync(toUpdate);

        Assert.Equal(OrderStatus.Confirmed, updated.Status);
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsOrdersMatchingIds()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context);
        Order order1 = await TestDataSeeder.CreateOrderAsync(context, user.Id);
        Order order2 = await TestDataSeeder.CreateOrderAsync(context, user.Id);

        OrderRepository repository = new OrderRepository(context);
        List<Order> results = await repository.GetByIdsAsync(new List<int> { order1.Id, order2.Id });

        Assert.Equal(2, results.Count);
    }
}
