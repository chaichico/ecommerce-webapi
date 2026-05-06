using Data;
using Ecommerce.Tests.Helpers;
using Models.Entities;
using Models.Enums;
using Repositories;

namespace Ecommerce.Tests.Repositories;

public class OrderRepositoryTests
{
    [Fact]
    public async Task Create_ShouldPersistOrderWithItems()
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
        await repository.Create(order);

        Assert.True(order.Id > 0);
        Assert.Equal("ORD-TEST-001", order.OrderNumber);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task GetByOrderId_WhenOrderExists_ReturnsOrderWithItems()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context);
        Product product = await TestDataSeeder.CreateProductAsync(context);
        Order created = await TestDataSeeder.CreateOrderWithItemsAsync(context, user.Id, product);

        OrderRepository repository = new OrderRepository(context);
        Order? result = await repository.GetByOrderId(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.NotEmpty(result.Items);
        Assert.NotNull(result.User);
    }

    [Fact]
    public async Task GetByOrderId_WhenOrderNotExists_ReturnsNull()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();

        OrderRepository repository = new OrderRepository(context);
        Order? result = await repository.GetByOrderId(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchOrders_ByOrderNumber_ReturnsMatchingOrders()
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
        List<Order> results = await repository.SearchOrders("SEARCH", null, null);

        Assert.Single(results);
        Assert.Equal("ORD-SEARCH-ABC", results[0].OrderNumber);
    }

    [Fact]
    public async Task Update_ShouldPersistChanges()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context);
        Order order = await TestDataSeeder.CreateOrderAsync(context, user.Id);

        OrderRepository repository = new OrderRepository(context);
        Order? toUpdate = await repository.GetByOrderId(order.Id);
        Assert.NotNull(toUpdate);

        toUpdate.Status = OrderStatus.Confirmed;
        await repository.Update(toUpdate);

        Assert.Equal(OrderStatus.Confirmed, toUpdate.Status);
    }

    [Fact]
    public async Task GetByIds_ReturnsOrdersMatchingIds()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context);
        Order order1 = await TestDataSeeder.CreateOrderAsync(context, user.Id);
        Order order2 = await TestDataSeeder.CreateOrderAsync(context, user.Id);

        OrderRepository repository = new OrderRepository(context);
        List<Order> results = await repository.GetByIds(new List<int> { order1.Id, order2.Id });

        Assert.Equal(2, results.Count);
    }
}
