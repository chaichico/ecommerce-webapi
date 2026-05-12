using AutoMapper;
using Data;
using Models.Dtos.Responses;
using Models.Entities;
using Moq;
using Repositories.Interfaces;
using Services;

namespace Ecommerce.Tests.Services;

public class OrderServiceTests
{
    // ── Shared mock fields ──────────────────────────────────────────────
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(
            _orderRepoMock.Object,
            _userRepoMock.Object,
            _productRepoMock.Object,
            _mapperMock.Object,
            _unitOfWorkMock.Object);
    }

    // ── GetOrderByIdAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderByIdAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetByEmail("ghost@example.com"))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetOrderByIdAsync(1, "ghost@example.com"));
    }

    [Fact]
    public async Task GetOrderByIdAsync_OrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(99)).ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetOrderByIdAsync(99, "user@example.com"));
    }

    [Fact]
    public async Task GetOrderByIdAsync_NotOwner_ThrowsSecurityException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        Order order = new Order { Id = 5, UserId = 99 };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<System.Security.SecurityException>(
            () => _sut.GetOrderByIdAsync(5, "user@example.com"));
    }

    [Fact]
    public async Task GetOrderByIdAsync_ValidOwner_ReturnsOrderResponseDto()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        Order order = new Order { Id = 5, UserId = 1, OrderNumber = "ORD-001" };
        OrderResponseDto expectedDto = new OrderResponseDto { Id = 5, OrderNumber = "ORD-001" };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);
        _mapperMock.Setup(m => m.Map<OrderResponseDto>(order)).Returns(expectedDto);

        // Act
        OrderResponseDto result = await _sut.GetOrderByIdAsync(5, "user@example.com");

        // Assert
        Assert.Equal("ORD-001", result.OrderNumber);
    }
}
