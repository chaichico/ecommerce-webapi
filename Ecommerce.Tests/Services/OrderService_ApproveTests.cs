using AutoMapper;
using Data;
using Models.Dtos.Requests;
using Models.Dtos.Responses;
using Models.Entities;
using Models.Enums;
using Moq;
using Repositories.Interfaces;
using Services;

namespace Ecommerce.Tests.Services;

public class OrderService_ApproveTests
{
    // ── Shared mock fields ──────────────────────────────────────────────
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly OrderService _sut;

    public OrderService_ApproveTests()
    {
        _sut = new OrderService(
            _orderRepoMock.Object,
            _userRepoMock.Object,
            _productRepoMock.Object,
            _mapperMock.Object,
            _unitOfWorkMock.Object);
    }

    // ── ApproveOrdersAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ApproveOrdersAsync_DuplicateOrderIds_ThrowsInvalidOperationException()
    {
        // Arrange
        ApproveOrdersDto dto = new ApproveOrdersDto { OrderIds = new List<int> { 1, 2, 1 } };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ApproveOrdersAsync(dto));
    }

    [Fact]
    public async Task ApproveOrdersAsync_OrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        ApproveOrdersDto dto = new ApproveOrdersDto { OrderIds = new List<int> { 1, 99 } };

        // GetByIds returns only one order — 99 is missing
        _orderRepoMock
            .Setup(r => r.GetByIds(dto.OrderIds))
            .ReturnsAsync(new List<Order> { new Order { Id = 1, Status = OrderStatus.Confirmed } });

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.ApproveOrdersAsync(dto));
    }

    [Fact]
    public async Task ApproveOrdersAsync_PendingOrder_ThrowsInvalidOperationException()
    {
        // Arrange
        ApproveOrdersDto dto = new ApproveOrdersDto { OrderIds = new List<int> { 1, 2 } };

        _orderRepoMock
            .Setup(r => r.GetByIds(dto.OrderIds))
            .ReturnsAsync(new List<Order>
            {
                new Order { Id = 1, Status = OrderStatus.Confirmed },
                new Order { Id = 2, Status = OrderStatus.Pending }
            });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ApproveOrdersAsync(dto));
    }

    [Fact]
    public async Task ApproveOrdersAsync_AlreadyApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        ApproveOrdersDto dto = new ApproveOrdersDto { OrderIds = new List<int> { 1, 2 } };

        _orderRepoMock
            .Setup(r => r.GetByIds(dto.OrderIds))
            .ReturnsAsync(new List<Order>
            {
                new Order { Id = 1, Status = OrderStatus.Confirmed },
                new Order { Id = 2, Status = OrderStatus.Approved }
            });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ApproveOrdersAsync(dto));
    }

    [Fact]
    public async Task ApproveOrdersAsync_AllConfirmedOrders_ReturnsApprovedDtos()
    {
        // Arrange
        ApproveOrdersDto dto = new ApproveOrdersDto { OrderIds = new List<int> { 1, 2 } };

        List<Order> orders = new List<Order>
        {
            new Order { Id = 1, Status = OrderStatus.Confirmed },
            new Order { Id = 2, Status = OrderStatus.Confirmed }
        };

        List<AdminOrderResponseDto> expectedDtos = new List<AdminOrderResponseDto>
        {
            new AdminOrderResponseDto { Status = OrderStatus.Approved },
            new AdminOrderResponseDto { Status = OrderStatus.Approved }
        };

        _orderRepoMock.Setup(r => r.GetByIds(dto.OrderIds)).ReturnsAsync(orders);
        _orderRepoMock.Setup(r => r.UpdateRange(It.IsAny<List<Order>>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(m => m.Map<List<AdminOrderResponseDto>>(orders)).Returns(expectedDtos);

        // Act
        List<AdminOrderResponseDto> result = await _sut.ApproveOrdersAsync(dto);

        // Assert
        Assert.Equal(2, result.Count);
        _orderRepoMock.Verify(r => r.UpdateRange(It.IsAny<List<Order>>()), Times.Once);
        Assert.All(orders, o => Assert.Equal(OrderStatus.Approved, o.Status));
    }

    // ── SearchOrdersAsync ────────────────────────────────────────────────

    [Fact]
    public async Task SearchOrdersAsync_WithResults_ReturnsAdminOrderResponseDtos()
    {
        // Arrange
        List<Order> orders = new List<Order>
        {
            new Order { Id = 1, OrderNumber = "ORD-001" },
            new Order { Id = 2, OrderNumber = "ORD-002" }
        };

        List<AdminOrderResponseDto> expectedDtos = new List<AdminOrderResponseDto>
        {
            new AdminOrderResponseDto { OrderNumber = "ORD-001" },
            new AdminOrderResponseDto { OrderNumber = "ORD-002" }
        };

        _orderRepoMock
            .Setup(r => r.SearchOrders("ORD", "John", null))
            .ReturnsAsync(orders);
        _mapperMock
            .Setup(m => m.Map<List<AdminOrderResponseDto>>(orders))
            .Returns(expectedDtos);

        // Act
        List<AdminOrderResponseDto> result = await _sut.SearchOrdersAsync("ORD", "John", null);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("ORD-001", result[0].OrderNumber);
    }

    [Fact]
    public async Task SearchOrdersAsync_EmptyResults_ReturnsEmptyList()
    {
        // Arrange
        List<Order> emptyOrders = new List<Order>();
        List<AdminOrderResponseDto> emptyDtos = new List<AdminOrderResponseDto>();

        _orderRepoMock
            .Setup(r => r.SearchOrders(null, null, null))
            .ReturnsAsync(emptyOrders);
        _mapperMock
            .Setup(m => m.Map<List<AdminOrderResponseDto>>(emptyOrders))
            .Returns(emptyDtos);

        // Act
        List<AdminOrderResponseDto> result = await _sut.SearchOrdersAsync(null, null, null);

        // Assert
        Assert.Empty(result);
    }
}
