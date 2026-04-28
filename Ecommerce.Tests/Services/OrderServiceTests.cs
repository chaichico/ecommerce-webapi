using Data;
using Ecommerce.Tests.Helpers;
using Models;
using Models.Dtos;
using Repositories;
using Services;

namespace Ecommerce.Tests.Services;

public class OrderServiceTests
{
    private static OrderService BuildService(AppDbContext context)
    {
        OrderRepository orderRepository = new OrderRepository(context);
        UserRepository userRepository = new UserRepository(context);
        ProductRepository productRepository = new ProductRepository(context);
        return new OrderService(orderRepository, userRepository, productRepository);
    }

    [Fact]
    public async Task CreateOrderAsync_WithValidData_ReturnsOrderResponseDto()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context, "orderuser@example.com");
        Product product = await TestDataSeeder.CreateProductAsync(context, "Widget", 50.00m);

        OrderService service = BuildService(context);

        CreateOrderDto dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = product.Id, Quantity = 2 }
            }
        };

        OrderResponseDto result = await service.CreateOrderAsync(dto, "orderuser@example.com");

        Assert.NotEmpty(result.OrderNumber);
        Assert.Equal("Pending", result.Status);
        Assert.Equal(100.00m, result.TotalPrice);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task CreateOrderAsync_WithInactiveProduct_ThrowsException()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        await TestDataSeeder.CreateUserAsync(context, "orderuser2@example.com");
        Product inactive = await TestDataSeeder.CreateProductAsync(context, "Inactive", 10m, isActive: false);

        OrderService service = BuildService(context);

        CreateOrderDto dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = inactive.Id, Quantity = 1 }
            }
        };

        await Assert.ThrowsAsync<Exception>(() => service.CreateOrderAsync(dto, "orderuser2@example.com"));
    }

    [Fact]
    public async Task UpdateOrderAsync_WithValidData_UpdatesOrderItems()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context, "updateuser@example.com");
        Product product = await TestDataSeeder.CreateProductAsync(context, "Widget", 50.00m);
        Order order = await TestDataSeeder.CreateOrderWithItemsAsync(context, user.Id, product, 1);

        OrderService service = BuildService(context);

        UpdateOrderDto dto = new UpdateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = product.Id, Quantity = 5 }
            }
        };

        OrderResponseDto result = await service.UpdateOrderAsync(order.Id, dto, "updateuser@example.com");

        Assert.Equal(250.00m, result.TotalPrice);
        Assert.Equal(5, result.Items[0].Quantity);
    }

    [Fact]
    public async Task UpdateOrderAsync_WhenNotOwner_ThrowsSecurityException()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User owner = await TestDataSeeder.CreateUserAsync(context, "owner@example.com");
        User other = await TestDataSeeder.CreateUserAsync(context, "other@example.com");
        Product product = await TestDataSeeder.CreateProductAsync(context);
        Order order = await TestDataSeeder.CreateOrderWithItemsAsync(context, owner.Id, product);

        OrderService service = BuildService(context);

        UpdateOrderDto dto = new UpdateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = product.Id, Quantity = 1 }
            }
        };

        await Assert.ThrowsAsync<System.Security.SecurityException>(
            () => service.UpdateOrderAsync(order.Id, dto, "other@example.com"));
    }

    [Fact]
    public async Task ConfirmOrderAsync_WithSufficientStock_UpdatesStatusAndDeductsStock()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context, "confirm@example.com");
        Product product = await TestDataSeeder.CreateProductAsync(context, "Widget", 100m);
        product.Stock = 10;
        await context.SaveChangesAsync();

        Order order = await TestDataSeeder.CreateOrderWithItemsAsync(context, user.Id, product, 3);
        OrderService service = BuildService(context);

        ConfirmOrderDto dto = new ConfirmOrderDto { ShippingAddress = "123 Test Street" };
        OrderResponseDto result = await service.ConfirmOrderAsync(order.Id, dto, "confirm@example.com");

        Assert.Equal("Confirmed", result.Status);

        Product? updatedProduct = await context.Products.FindAsync(product.Id);
        Assert.NotNull(updatedProduct);
        Assert.Equal(7, updatedProduct.Stock);
    }

    [Fact]
    public async Task ConfirmOrderAsync_WithInsufficientStock_ThrowsInvalidOperationException()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context, "stockfail@example.com");
        Product product = await TestDataSeeder.CreateProductAsync(context, "Widget", 100m);
        product.Stock = 1;
        await context.SaveChangesAsync();

        Order order = await TestDataSeeder.CreateOrderWithItemsAsync(context, user.Id, product, 5);
        OrderService service = BuildService(context);

        ConfirmOrderDto dto = new ConfirmOrderDto { ShippingAddress = "123 Test Street" };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ConfirmOrderAsync(order.Id, dto, "stockfail@example.com"));
    }
}
