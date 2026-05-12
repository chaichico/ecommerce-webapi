# Unit Test Guide — xUnit + Moq

## สารบัญ
1. [ทำไมต้องใช้ Moq ไม่ใช่ InMemory DB](#1-ทำไมตองใช-moq-ไมใช-inmemory-db)
2. [ติดตั้ง Package](#2-ติดตั้ง-package)
3. [โครงสร้างโปรเจกต์ Test](#3-โครงสรางโปรเจกต-test)
4. [Naming Convention](#4-naming-convention)
5. [AAA Pattern](#5-aaa-pattern)
6. [วิธี Setup Mock ด้วย Moq](#6-วิธี-setup-mock-ดวย-moq)
7. [Test Coverage Plan — OrderService](#7-test-coverage-plan--orderservice)
8. [Test Coverage Plan — UserService](#8-test-coverage-plan--userservice)
9. [ตัวอย่าง Code จริง](#9-ตัวอยาง-code-จริง)
10. [Implementation Phases](#10-implementation-phases)

---

## 1. ทำไมต้องใช้ Moq ไม่ใช่ InMemory DB 

| เรื่อง | InMemory DB | Moq (Unit Test) |
|---|---|---|
| ประเภท | Integration Test | Unit Test |
| ทดสอบอะไร | EF Core + DB Layer + Service ไปพร้อมกัน | Business Logic เพียวๆ |
| ความเร็ว | ช้ากว่า (สร้าง Context ทุก test) | เร็วมาก |
| Isolation | ไม่ isolated — มี side effect ระหว่าง layer | Isolated ทุก dependency |
| เข้าถึงได้แค่ไหน | เฉพาะ scenario ที่ EF Core รองรับ | ควบคุม return value ได้ทุกอย่าง |
| วัตถุประสงค์ | ตรวจสอบว่า query/mapping ถูกต้อง | ตรวจสอบว่า logic ใน Service ถูกต้อง |

**กฎหลัก**: Unit test ทดสอบ **Service** ทุกโดย mock ทุก dependency  
Repository, Mapper, UnitOfWork — ทั้งหมดถูก mock หมด ไม่มี DB จริง

---

## 2. ติดตั้ง Package [x]

เพิ่มใน `Ecommerce.Tests/Ecommerce.Tests.csproj`:

```xml
<PackageReference Include="Moq" Version="4.20.72" />
```

ติดตั้งผ่าน terminal:

```bash
dotnet add Ecommerce.Tests/Ecommerce.Tests.csproj package Moq
```

---

## 3. โครงสร้างโปรเจกต์ Test

```
Ecommerce.Tests/
├── Services/
│   ├── OrderServiceTests.cs          ← GetOrderById, CreateOrder, UpdateOrder, ConfirmOrder
│   ├── OrderService_ApproveTests.cs  ← ApproveOrders (แยกเพราะหลาย case)
│   └── UserServiceTests.cs           ← Register, Login
└── Ecommerce.Tests.csproj
```

**กฎ**: ไม่มี `Helpers/TestDataSeeder`, ไม่มี `Fakes/`, ไม่มี `InMemory DB`  
ทุกอย่างสร้างใน `Arrange` ของ test นั้นเอง

---

## 4. Naming Convention

### Class
```
{ServiceName}Tests
```
เช่น `OrderServiceTests`, `UserServiceTests`

### Method
```
{MethodName}_{Scenario}_{ExpectedOutcome}
```
เช่น:
- `GetOrderByIdAsync_OrderNotFound_ThrowsKeyNotFoundException`
- `CreateOrderAsync_ValidData_ReturnsOrderResponseDto`
- `LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException`

**หลักการตั้งชื่อ Scenario:**
- Happy path → `ValidData`, `WithPhone`, `ConfirmedOrders`
- Error / edge case → `OrderNotFound`, `NotOwner`, `InsufficientStock`, `DuplicateEmail`

---

## 5. AAA Pattern

```csharp
[Fact]
public async Task MethodName_Scenario_Expected()
{
    // Arrange — เตรียมทุกอย่างก่อน: mock setup, input data, expected values
    // ...

    // Act — เรียก method เดียว บรรทัดเดียว ไม่มีอะไรอื่น
    // ...

    // Assert — ตรวจผลลัพธ์ ควร assert สิ่งเดียวต่อ test
    // ...
}
```

### กฎของแต่ละ section

**Arrange**
- Mock setup ทุก dependency
- สร้าง input DTO / entity
- กำหนด expected value ถ้าต้องการ

**Act**
- เรียก method เดียว
- ถ้า expect exception ใช้ `await Assert.ThrowsAsync<T>(() => ...)` ใน Assert section แทน

**Assert**
- Assert **สิ่งเดียว** ต่อ test (หรือกลุ่มที่เกี่ยวกันโดยตรง)
- ใช้ `_mock.Verify(...)` เพื่อตรวจว่า method ถูกเรียกหรือไม่

---

## 6. วิธี Setup Mock ด้วย Moq

### ติดตั้ง Mock

```csharp
Mock<IOrderRepository> orderRepoMock = new Mock<IOrderRepository>();
Mock<IUserRepository> userRepoMock = new Mock<IUserRepository>();
Mock<IProductRepository> productRepoMock = new Mock<IProductRepository>();
Mock<IMapper> mapperMock = new Mock<IMapper>();
Mock<IUnitOfWork> unitOfWorkMock = new Mock<IUnitOfWork>();
```

### สร้าง Service

```csharp
OrderService sut = new OrderService(
    orderRepoMock.Object,
    userRepoMock.Object,
    productRepoMock.Object,
    mapperMock.Object,
    unitOfWorkMock.Object);
```

> **sut** = System Under Test — ชื่อ convention มาตรฐาน

### Setup Return Value

```csharp
// Return value
userRepoMock
    .Setup(r => r.GetByEmail("test@example.com"))
    .ReturnsAsync(new User { Id = 1, Email = "test@example.com" });

// Return null (ไม่พบ)
orderRepoMock
    .Setup(r => r.GetByOrderId(99))
    .ReturnsAsync((Order?)null);

// Return list
productRepoMock
    .Setup(r => r.GetActiveByIds(It.IsAny<List<int>>()))
    .ReturnsAsync(new List<Product> { ... });
```

### Setup Task (void return)

```csharp
orderRepoMock
    .Setup(r => r.Create(It.IsAny<Order>()))
    .Returns(Task.CompletedTask);
```

### Setup ExecuteInTransactionAsync (IUnitOfWork)

```csharp
unitOfWorkMock
    .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
    .Returns<Func<Task>>(fn => fn());  // เรียก callback จริง
```

### Verify ว่าถูกเรียก

```csharp
// ต้องถูกเรียก 1 ครั้ง
orderRepoMock.Verify(r => r.Create(It.IsAny<Order>()), Times.Once);

// ต้องไม่ถูกเรียกเลย
orderRepoMock.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
```

### It.IsAny vs ค่าจริง

```csharp
// ใช้ค่าจริงเมื่อต้องการเฉพาะเจาะจง
.Setup(r => r.GetByEmail("test@example.com"))

// ใช้ It.IsAny เมื่อไม่สนใจค่า (แค่ต้อง return บางอย่าง)
.Setup(r => r.Create(It.IsAny<Order>()))
```

---

## 7. Test Coverage Plan — OrderService

### GetOrderByIdAsync(id, userEmail)

| # | Scenario | Expected |
|---|---|---|
| 1 | User email ไม่พบใน DB | `KeyNotFoundException` |
| 2 | Order ไม่พบ | `KeyNotFoundException` |
| 3 | Order เป็นของ user อื่น | `SecurityException` |
| 4 | Owner ขอดู order ตัวเอง | returns `OrderResponseDto` |

### CreateOrderAsync(dto, userEmail)

| # | Scenario | Expected |
|---|---|---|
| 1 | User ไม่พบ | `KeyNotFoundException` |
| 2 | Product บางตัวไม่ active หรือไม่มีใน DB | `InvalidOperationException` |
| 3 | ข้อมูลถูกต้องครบ | returns `OrderResponseDto`, Status = Pending, TotalPrice ถูกต้อง |
| 4 | `IOrderRepository.Create` ถูกเรียก 1 ครั้ง | Verify `Times.Once` |

### UpdateOrderAsync(id, dto, userEmail)

| # | Scenario | Expected |
|---|---|---|
| 1 | User ไม่พบ | `KeyNotFoundException` |
| 2 | Order ไม่พบ | `KeyNotFoundException` |
| 3 | User ไม่ใช่เจ้าของ | `SecurityException` |
| 4 | Order ไม่ใช่ Pending | `InvalidOperationException` |
| 5 | Product ไม่พบ/inactive | `InvalidOperationException` |
| 6 | ข้อมูลถูกต้อง | returns `OrderResponseDto` |

### ConfirmOrderAsync(id, dto, userEmail)

| # | Scenario | Expected |
|---|---|---|
| 1 | User ไม่พบ | `KeyNotFoundException` |
| 2 | Order ไม่พบ | `KeyNotFoundException` |
| 3 | User ไม่ใช่เจ้าของ | `SecurityException` |
| 4 | Order ไม่ใช่ Pending | `InvalidOperationException` |
| 5 | Stock ไม่พอ (product.Stock < item.Quantity) | `InvalidOperationException` |
| 6 | ข้อมูลถูกต้อง Stock พอ | Status = Confirmed, ShippingAddress บันทึก, returns DTO |

### ApproveOrdersAsync(dto)

| # | Scenario | Expected |
|---|---|---|
| 1 | มี Duplicate OrderIds ใน request | `InvalidOperationException` |
| 2 | บาง Order ไม่พบใน DB | `KeyNotFoundException` |
| 3 | มี Order ที่ยัง Pending (ยัง confirm ไม่ได้) | `InvalidOperationException` |
| 4 | มี Order ที่ Approved ไปแล้ว | `InvalidOperationException` |
| 5 | Orders ทั้งหมด Confirmed | Status = Approved, returns list DTOs |

### SearchOrdersAsync(orderNumber, firstName, lastName)

> method นี้ไม่มี business rule — เป็น pass-through ไปยัง repository แล้ว map result

| # | Scenario | Expected |
|---|---|---|
| 1 | Repository คืน list ที่มี orders | returns `List<AdminOrderResponseDto>` ที่ map ถูกต้อง |
| 2 | Repository คืน list ว่าง | returns `List<AdminOrderResponseDto>` ว่าง ไม่ throw |

---

## 8. Test Coverage Plan — UserService

### RegisterAsync(dto)

| # | Scenario | Expected |
|---|---|---|
| 1 | Email มีอยู่แล้วใน DB | `InvalidOperationException` |
| 2 | มี PhoneNumber | `_encryptionService.Encrypt()` ถูกเรียก 1 ครั้ง |
| 3 | ไม่มี PhoneNumber (null/empty) | `_encryptionService.Encrypt()` ไม่ถูกเรียก |
| 4 | ข้อมูลถูกต้อง | returns `UserResponseDto`, `_passwordHasher.HashPassword()` ถูกเรียก |

### LoginAsync(dto)

| # | Scenario | Expected |
|---|---|---|
| 1 | Email ไม่พบ | `UnauthorizedAccessException` |
| 2 | Password ผิด (`VerifyPassword` returns false) | `UnauthorizedAccessException` |
| 3 | Email + Password ถูก | returns `LoginResponseDto` มี Token |

---

## 9. ตัวอย่าง Code จริง

### OrderServiceTests.cs (skeleton)

```csharp
using AutoMapper;
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
    public async Task GetOrderByIdAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetByEmail("ghost@example.com"))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
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
        Order order = new Order { Id = 5, UserId = 99 }; // เจ้าของเป็น userId = 99

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
    public async Task CreateOrderAsync_UserNotFound_ThrowsKeyNotFoundException()
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
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.CreateOrderAsync(dto, "ghost@example.com"));
    }

    [Fact]
    public async Task CreateOrderAsync_ProductNotActive_ThrowsInvalidOperationException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com" };
        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);

        // GetActiveByIds คืน list ว่าง (product ไม่ active)
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
        OrderResponseDto result = await _sut.CreateOrderAsync(dto, "user@example.com");

        // Assert
        _orderRepoMock.Verify(r => r.Create(It.IsAny<Order>()), Times.Once);
    }

    // ── ConfirmOrderAsync ────────────────────────────────────────────────

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

        Product product = new Product { Id = 1, Stock = 3 }; // Stock น้อยกว่า Quantity

        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _orderRepoMock.Setup(r => r.GetByOrderId(5)).ReturnsAsync(order);
        _productRepoMock
            .Setup(r => r.GetByIds(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<Product> { product });

        ConfirmOrderDto dto = new ConfirmOrderDto { ShippingAddress = "123 Test St" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ConfirmOrderAsync(5, dto, "user@example.com"));
    }
}
```

### UserServiceTests.cs (skeleton)

```csharp
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Models.Dtos.Requests;
using Models.Dtos.Responses;
using Models.Entities;
using Moq;
using Repositories.Interfaces;
using Services;
using Services.Interfaces;

namespace Ecommerce.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IEncryptionService> _encryptionMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly IConfiguration _configuration;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        Dictionary<string, string?> configValues = new Dictionary<string, string?>
        {
            { "Jwt:Key", "super-secret-key-for-testing-only-32chars!!" },
            { "Jwt:Issuer", "test-issuer" },
            { "Jwt:Audience", "test-audience" },
            { "Jwt:ExpiryInMinutes", "60" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _sut = new UserService(
            _userRepoMock.Object,
            _passwordHasherMock.Object,
            _encryptionMock.Object,
            _configuration,
            _mapperMock.Object);
    }

    // ── RegisterAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.EmailExists("dup@example.com"))
            .ReturnsAsync(true);

        RegisterUserDto dto = new RegisterUserDto
        {
            Email = "dup@example.com",
            FirstName = "John",
            LastName = "Doe",
            Password = "pass123",
            ConfirmPassword = "pass123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RegisterAsync(dto));
    }

    [Fact]
    public async Task RegisterAsync_WithPhoneNumber_CallsEncryptOnce()
    {
        // Arrange
        _userRepoMock.Setup(r => r.EmailExists(It.IsAny<string>())).ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed");
        _encryptionMock.Setup(e => e.Encrypt("0812345678")).Returns("encrypted_phone");
        _userRepoMock.Setup(r => r.Create(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(m => m.Map<UserResponseDto>(It.IsAny<User>())).Returns(new UserResponseDto());

        RegisterUserDto dto = new RegisterUserDto
        {
            Email = "new@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            Password = "pass123",
            ConfirmPassword = "pass123",
            PhoneNumber = "0812345678"
        };

        // Act
        await _sut.RegisterAsync(dto);

        // Assert
        _encryptionMock.Verify(e => e.Encrypt("0812345678"), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithoutPhoneNumber_DoesNotCallEncrypt()
    {
        // Arrange
        _userRepoMock.Setup(r => r.EmailExists(It.IsAny<string>())).ReturnsAsync(false);
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<string>())).Returns("hashed");
        _userRepoMock.Setup(r => r.Create(It.IsAny<User>())).Returns(Task.CompletedTask);
        _mapperMock.Setup(m => m.Map<UserResponseDto>(It.IsAny<User>())).Returns(new UserResponseDto());

        RegisterUserDto dto = new RegisterUserDto
        {
            Email = "new@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            Password = "pass123",
            ConfirmPassword = "pass123",
            PhoneNumber = null
        };

        // Act
        await _sut.RegisterAsync(dto);

        // Assert
        _encryptionMock.Verify(e => e.Encrypt(It.IsAny<string>()), Times.Never);
    }

    // ── LoginAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_EmailNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetByEmail("nobody@example.com"))
            .ReturnsAsync((User?)null);

        LoginDto dto = new LoginDto { Email = "nobody@example.com", Password = "anypass" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        User user = new User { Id = 1, Email = "user@example.com", PasswordHash = "correct_hash" };
        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.VerifyPassword("wrongpass", "correct_hash"))
            .Returns(false);

        LoginDto dto = new LoginDto { Email = "user@example.com", Password = "wrongpass" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponseDto()
    {
        // Arrange
        User user = new User
        {
            Id = 1,
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = "correct_hash"
        };
        _userRepoMock.Setup(r => r.GetByEmail("user@example.com")).ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correctpass", "correct_hash"))
            .Returns(true);
        _mapperMock.Setup(m => m.Map<UserResponseDto>(user)).Returns(new UserResponseDto { Email = "user@example.com" });

        LoginDto dto = new LoginDto { Email = "user@example.com", Password = "correctpass" };

        // Act
        LoginResponseDto result = await _sut.LoginAsync(dto);

        // Assert
        Assert.NotEmpty(result.Token);
    }
}
```

---

## Quick Reference

### Mock pattern ที่ใช้บ่อย

```csharp
// Return object
mock.Setup(r => r.GetByEmail("x")).ReturnsAsync(new User { ... });

// Return null
mock.Setup(r => r.GetByOrderId(0)).ReturnsAsync((Order?)null);

// Return Task (void)
mock.Setup(r => r.Create(It.IsAny<Order>())).Returns(Task.CompletedTask);

// ExecuteInTransactionAsync — ต้องเรียก callback จริง
unitOfWorkMock
    .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
    .Returns<Func<Task>>(fn => fn());

// Verify ถูกเรียก
mock.Verify(r => r.Create(It.IsAny<Order>()), Times.Once);
mock.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
```

### Error message เมื่อ Mock ไม่ครบ

```
System.NotSupportedException: Unsupported expression...
```
→ ตรวจว่า setup ครบทุก method ที่ service เรียก

```
Moq.MockException: Expected invocation on the mock once, but was 0 times
```
→ Logic ใน service ไม่ได้เรียก method นั้น ตรวจสอบ flow ใหม่

---

## 10. Implementation Phases

> สถานะปัจจุบัน: 29 tests (28 pass / 1 fail), ใช้ InMemory DB — ต้องแปลงเป็น Moq pure unit tests

### สรุปไฟล์ที่มีอยู่

| ไฟล์ | Tests | หมายเหตุ |
|---|---|---|
| `Services/OrderServiceTests.cs` | 8 | InMemory DB + TestDataSeeder |
| `Services/ApproveOrdersAsyncTests.cs` | 3 | InMemory DB |
| `Services/UserServiceTests.cs` | 5 | InMemory DB + Fakes |
| `Repositories/*.cs` | 13 | Integration tests — ไม่อยู่ใน scope |
| `Fakes/`, `Helpers/` | — | ต้องลบออก |
| ❌ `UpdateOrderAsync_WithValidData` | FAIL | InMemory ไม่รองรับ Transaction |

---

### Phase 1 — ปรับโครงสร้างและติดตั้ง Moq

> เป้าหมาย: โปรเจกต์ build ได้ด้วย Moq, ลบโครงสร้างเก่าทิ้ง

- [x] `dotnet add Ecommerce.Tests/ package Moq`
- [x] ลบ `PackageReference` ของ `Microsoft.EntityFrameworkCore.InMemory` ออกจาก `.csproj`
- [x] ลบไฟล์ทั้งหมดใน `Fakes/`
- [x] ลบไฟล์ทั้งหมดใน `Helpers/`
- [x] ลบ `UnitTest1.cs`
- [x] แทนที่ `Services/OrderServiceTests.cs` ด้วย skeleton ใหม่ (mock fields + constructor)
- [x] แทนที่ `Services/ApproveOrdersAsyncTests.cs` → rename เป็น `OrderService_ApproveTests.cs` พร้อม skeleton
- [x] แทนที่ `Services/UserServiceTests.cs` ด้วย skeleton ใหม่
- [x] `dotnet build` ผ่านไม่มี error

---

### Phase 2 — OrderServiceTests: `GetOrderByIdAsync` (4 tests)

- [ ] `GetOrderByIdAsync_UserNotFound_ThrowsKeyNotFoundException`
- [ ] `GetOrderByIdAsync_OrderNotFound_ThrowsKeyNotFoundException`
- [ ] `GetOrderByIdAsync_NotOwner_ThrowsSecurityException`
- [ ] `GetOrderByIdAsync_ValidOwner_ReturnsOrderResponseDto`
- [ ] `dotnet test` → 4/4 pass

---

### Phase 3 — OrderServiceTests: `CreateOrderAsync` (4 tests)

- [ ] `CreateOrderAsync_UserNotFound_ThrowsKeyNotFoundException`
- [ ] `CreateOrderAsync_ProductNotActive_ThrowsInvalidOperationException`
- [ ] `CreateOrderAsync_ValidData_ReturnsOrderResponseDto` (Status=Pending, TotalPrice ถูก)
- [ ] `CreateOrderAsync_ValidData_CallsRepositoryCreateOnce` (Verify Times.Once)
- [ ] `dotnet test` → 4/4 pass

---

### Phase 4 — OrderServiceTests: `UpdateOrderAsync` (6 tests)

- [ ] `UpdateOrderAsync_UserNotFound_ThrowsKeyNotFoundException`
- [ ] `UpdateOrderAsync_OrderNotFound_ThrowsKeyNotFoundException`
- [ ] `UpdateOrderAsync_NotOwner_ThrowsSecurityException`
- [ ] `UpdateOrderAsync_NotPending_ThrowsInvalidOperationException`
- [ ] `UpdateOrderAsync_ProductNotFound_ThrowsInvalidOperationException`
- [ ] `UpdateOrderAsync_ValidData_ReturnsOrderResponseDto`
- [ ] `dotnet test` → 6/6 pass

---

### Phase 5 — OrderServiceTests: `ConfirmOrderAsync` (6 tests)

- [ ] `ConfirmOrderAsync_UserNotFound_ThrowsKeyNotFoundException`
- [ ] `ConfirmOrderAsync_OrderNotFound_ThrowsKeyNotFoundException`
- [ ] `ConfirmOrderAsync_NotOwner_ThrowsSecurityException`
- [ ] `ConfirmOrderAsync_NotPending_ThrowsInvalidOperationException`
- [ ] `ConfirmOrderAsync_InsufficientStock_ThrowsInvalidOperationException`
- [ ] `ConfirmOrderAsync_ValidData_StatusConfirmedAndReturnsDto`
- [ ] `dotnet test` → 6/6 pass

---

### Phase 6 — OrderService_ApproveTests: `ApproveOrdersAsync` + `SearchOrdersAsync` (7 tests)

- [ ] `ApproveOrdersAsync_DuplicateOrderIds_ThrowsInvalidOperationException`
- [ ] `ApproveOrdersAsync_OrderNotFound_ThrowsKeyNotFoundException`
- [ ] `ApproveOrdersAsync_PendingOrder_ThrowsInvalidOperationException`
- [ ] `ApproveOrdersAsync_AlreadyApproved_ThrowsInvalidOperationException`
- [ ] `ApproveOrdersAsync_AllConfirmedOrders_ReturnsApprovedDtos`
- [ ] `SearchOrdersAsync_WithResults_ReturnsAdminOrderResponseDtos`
- [ ] `SearchOrdersAsync_EmptyResults_ReturnsEmptyList`
- [ ] `dotnet test` → 7/7 pass

---

### Phase 7 — UserServiceTests: `RegisterAsync` + `LoginAsync` (7 tests)

- [ ] `RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException`
- [ ] `RegisterAsync_WithPhoneNumber_CallsEncryptOnce`
- [ ] `RegisterAsync_WithoutPhoneNumber_DoesNotCallEncrypt`
- [ ] `RegisterAsync_ValidData_CallsHashPasswordOnce`
- [ ] `LoginAsync_EmailNotFound_ThrowsUnauthorizedAccessException`
- [ ] `LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException`
- [ ] `LoginAsync_ValidCredentials_ReturnsLoginResponseDtoWithToken`
- [ ] `dotnet test` → 7/7 pass

---

### Phase 8 — Final Verification

- [ ] `dotnet test` ทั้ง solution → **34/34 pass** (20 OrderService + 7 Approve + 7 User)
- [ ] ตรวจ scenario ใน guide ครบทุกข้อ (sections 7–8)
- [ ] ไม่มี `using Data;`, `using Repositories;`, `InMemory`, `TestDataSeeder` เหลือในไฟล์ Services ใดๆ
