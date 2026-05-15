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

public class OrderServiceTests
{
    // 1. เตรียม Mock Object ยังไม่ได้ Inject
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly OrderService _sut; // ระบบที่เราจะ test

    // 2. constructor test
    public OrderServiceTests()
    {
        // 3. inject fake dependencies into the SUT (System Under Test)
        // SUT คือ OrderService ที่เราจะทดสอบนั่นเอง
        _sut = new OrderService(
            _orderRepoMock.Object,
            _userRepoMock.Object,
            _productRepoMock.Object,
            _mapperMock.Object,
            _unitOfWorkMock.Object);
    }

    // 4. รัน test 
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

    // ── CreateOrderAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrderAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetByEmail("ghost@example.com"))
            .ReturnsAsync((User?)null);

        CreateOrderDto dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CreateOrderAsync(dto, "ghost@example.com"));
    }

    [Fact]
    public async Task CreateOrderAsync_ProductNotActive_ThrowsInvalidOperationException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);

        _productRepoMock
            .Setup(r => r.GetActiveByIds(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product>());

        CreateOrderDto dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 10, Quantity = 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.CreateOrderAsync(dto, "user@example.com"));
    }

    [Fact]
    public async Task CreateOrderAsync_ValidData_ReturnsOrderResponseDto()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        Product product = new Product { Id = 1, ProductName = "Widget", Price = 50m, IsActive = true };
        OrderResponseDto expectedDto = new OrderResponseDto { Id = 1, TotalPrice = 100m };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _productRepoMock
            .Setup(r => r.GetActiveByIds(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product });
        _orderRepoMock
            .Setup(r => r.Create(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);
        _mapperMock
            .Setup(m => m.Map<OrderResponseDto>(It.IsAny<Order>()))
            .Returns(expectedDto);

        CreateOrderDto dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        OrderResponseDto result = await _sut.CreateOrderAsync(dto, "user@example.com");

        // Assert
        Assert.Equal(100m, result.TotalPrice);
    }

    [Fact]
    public async Task CreateOrderAsync_ValidData_CallsRepositoryCreateOnce()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        Product product = new Product { Id = 1, ProductName = "Widget", Price = 50m, IsActive = true };
        OrderResponseDto expectedDto = new OrderResponseDto { Id = 1, TotalPrice = 100m };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _productRepoMock
            .Setup(r => r.GetActiveByIds(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product });
        _orderRepoMock
            .Setup(r => r.Create(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);
        _mapperMock
            .Setup(m => m.Map<OrderResponseDto>(It.IsAny<Order>()))
            .Returns(expectedDto);

        CreateOrderDto dto = new CreateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        await _sut.CreateOrderAsync(dto, "user@example.com");

        // Assert
        _orderRepoMock.Verify(r => r.Create(It.IsAny<Order>()), Times.Once);
    }

    // ── UpdateOrderAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateOrderAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetByEmail("ghost@example.com"))
            .ReturnsAsync((User?)null);

        UpdateOrderDto dto = new UpdateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UpdateOrderAsync(1, dto, "ghost@example.com"));
    }

    [Fact]
    public async Task UpdateOrderAsync_OrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(99)).ReturnsAsync((Order?)null);

        UpdateOrderDto dto = new UpdateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.UpdateOrderAsync(99, dto, "user@example.com"));
    }

    [Fact]
    public async Task UpdateOrderAsync_NotOwner_ThrowsSecurityException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        Order order = new Order { Id = 5, UserId = 99, Status = OrderStatus.Pending };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);

        UpdateOrderDto dto = new UpdateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<System.Security.SecurityException>(
            () => _sut.UpdateOrderAsync(5, dto, "user@example.com"));
    }

    [Fact]
    public async Task UpdateOrderAsync_NotPending_ThrowsInvalidOperationException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        Order order = new Order { Id = 5, UserId = 1, Status = OrderStatus.Confirmed };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);

        UpdateOrderDto dto = new UpdateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateOrderAsync(5, dto, "user@example.com"));
    }

    [Fact]
    public async Task UpdateOrderAsync_ProductNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        Order order = new Order { Id = 5, UserId = 1, Status = OrderStatus.Pending, Items = new List<OrderItem>() };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);
        _productRepoMock
            .Setup(r => r.GetActiveByIds(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product>());

        UpdateOrderDto dto = new UpdateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 99, Quantity = 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateOrderAsync(5, dto, "user@example.com"));
    }

    [Fact]
    public async Task UpdateOrderAsync_ValidData_ReturnsOrderResponseDto()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        Order order = new Order
        {
            Id = 5,
            UserId = 1,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, ProductName = "OldWidget", Quantity = 1, UnitPrice = 10m }
            }
        };
        Product product = new Product { Id = 2, ProductName = "NewWidget", Price = 75m, IsActive = true };
        OrderResponseDto expectedDto = new OrderResponseDto { Id = 5, TotalPrice = 150m };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);
        _productRepoMock
            .Setup(r => r.GetActiveByIds(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product });
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(fn => fn());
        _orderRepoMock
            .Setup(r => r.RemoveItems(It.IsAny<List<OrderItem>>()))
            .Returns(Task.CompletedTask);
        _orderRepoMock
            .Setup(r => r.Update(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        _mapperMock
            .Setup(m => m.Map<OrderResponseDto>(It.IsAny<Order>()))
            .Returns(expectedDto);

        UpdateOrderDto dto = new UpdateOrderDto
        {
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 2, Quantity = 2 }
            }
        };

        // Act
        OrderResponseDto result = await _sut.UpdateOrderAsync(5, dto, "user@example.com");

        // Assert
        Assert.Equal(150m, result.TotalPrice);
    }

    // ── ConfirmOrderAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmOrderAsync_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetByEmail("ghost@example.com"))
            .ReturnsAsync((User?)null);

        ConfirmOrderDto dto = new ConfirmOrderDto { ShippingAddress = "123 Test Street City" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ConfirmOrderAsync(1, dto, "ghost@example.com"));
    }

    [Fact]
    public async Task ConfirmOrderAsync_OrderNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(99)).ReturnsAsync((Order?)null);

        ConfirmOrderDto dto = new ConfirmOrderDto { ShippingAddress = "123 Test Street City" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.ConfirmOrderAsync(99, dto, "user@example.com"));
    }

    [Fact]
    public async Task ConfirmOrderAsync_NotOwner_ThrowsSecurityException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        Order order = new Order { Id = 5, UserId = 99, Status = OrderStatus.Pending, Items = new List<OrderItem>() };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);

        ConfirmOrderDto dto = new ConfirmOrderDto { ShippingAddress = "123 Test Street City" };

        // Act & Assert
        await Assert.ThrowsAsync<System.Security.SecurityException>(
            () => _sut.ConfirmOrderAsync(5, dto, "user@example.com"));
    }

    [Fact]
    public async Task ConfirmOrderAsync_NotPending_ThrowsInvalidOperationException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        Order order = new Order { Id = 5, UserId = 1, Status = OrderStatus.Confirmed, Items = new List<OrderItem>() };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);

        ConfirmOrderDto dto = new ConfirmOrderDto { ShippingAddress = "123 Test Street City" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ConfirmOrderAsync(5, dto, "user@example.com"));
    }

    [Fact]
    public async Task ConfirmOrderAsync_InsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };

        OrderItem item = new OrderItem { ProductId = 1, ProductName = "Widget", Quantity = 10 };
        Order order = new Order
        {
            Id = 5,
            UserId = 1,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem> { item }
        };
        Product product = new Product { Id = 1, Stock = 3 };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);
        _productRepoMock
            .Setup(r => r.GetByIds(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product });

        ConfirmOrderDto dto = new ConfirmOrderDto { ShippingAddress = "123 Test Street City" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ConfirmOrderAsync(5, dto, "user@example.com"));
    }

    [Fact]
    public async Task ConfirmOrderAsync_ValidData_StatusConfirmedAndReturnsDto()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };

        OrderItem item = new OrderItem { ProductId = 1, ProductName = "Widget", Quantity = 2 };
        Order order = new Order
        {
            Id = 5,
            UserId = 1,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem> { item }
        };
        Product product = new Product { Id = 1, Stock = 10 };
        OrderResponseDto expectedDto = new OrderResponseDto { Id = 5, Status = OrderStatus.Confirmed };

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);
        _productRepoMock
            .Setup(r => r.GetByIds(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product });
        _orderRepoMock
            .Setup(r => r.Update(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);
        _mapperMock
            .Setup(m => m.Map<OrderResponseDto>(It.IsAny<Order>()))
            .Returns(expectedDto);

        ConfirmOrderDto dto = new ConfirmOrderDto { ShippingAddress = "123 Test Street City" };

        // Act
        OrderResponseDto result = await _sut.ConfirmOrderAsync(5, dto, "user@example.com");

        // Assert
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal("123 Test Street City", order.ShippingAddress);
        Assert.Equal(8, product.Stock); // 10 - 2
    }
}
