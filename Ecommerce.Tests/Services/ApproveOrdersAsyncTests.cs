using Data;
using Ecommerce.Tests.Helpers;
using Models;
using Models.Dtos;
using Repositories;
using Services;

namespace Ecommerce.Tests.Services;

public class ApproveOrdersAsyncTests
{
    private static OrderService BuildService(AppDbContext context)
    {
        OrderRepository orderRepository = new OrderRepository(context);
        UserRepository userRepository = new UserRepository(context);
        ProductRepository productRepository = new ProductRepository(context);
        return new OrderService(orderRepository, userRepository, productRepository);
    }

    [Fact]
    public async Task ApproveOrdersAsync_WithConfirmedOrders_ChangesStatusToApproved()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context, "approve-success@example.com");
        Product product = await TestDataSeeder.CreateProductAsync(context, "Approve Product", 20m);
        Order order = await TestDataSeeder.CreateOrderWithItemsAsync(context, user.Id, product, 2);

        order.Status = "Confirmed";
        await context.SaveChangesAsync();

        OrderService service = BuildService(context);

        ApproveOrdersDto dto = new ApproveOrdersDto
        {
            OrderIds = new List<int> { order.Id }
        };

        List<AdminOrderResponseDto> result = await service.ApproveOrdersAsync(dto);

        Assert.Single(result);
        Assert.Equal("Approved", result[0].Status);

        Order? updatedOrder = await context.Orders.FindAsync(order.Id);
        Assert.NotNull(updatedOrder);
        Assert.Equal("Approved", updatedOrder.Status);
    }

    [Fact]
    public async Task ApproveOrdersAsync_WithPendingOrders_ThrowsInvalidOperationException()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        User user = await TestDataSeeder.CreateUserAsync(context, "approve-pending@example.com");
        Product product = await TestDataSeeder.CreateProductAsync(context, "Pending Product", 30m);
        Order pendingOrder = await TestDataSeeder.CreateOrderWithItemsAsync(context, user.Id, product, 1);

        OrderService service = BuildService(context);

        ApproveOrdersDto dto = new ApproveOrdersDto
        {
            OrderIds = new List<int> { pendingOrder.Id }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveOrdersAsync(dto));
    }

    [Fact]
    public async Task ApproveOrdersAsync_WithUnknownOrderId_ThrowsKeyNotFoundException()
    {
        await using AppDbContext context = TestDbContextFactory.CreateFresh();
        OrderService service = BuildService(context);

        ApproveOrdersDto dto = new ApproveOrdersDto
        {
            OrderIds = new List<int> { 99999 }
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ApproveOrdersAsync(dto));
    }
}
