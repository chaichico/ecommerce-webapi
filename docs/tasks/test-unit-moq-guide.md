# Unit Test Guide — xUnit + Moq

---

## กฎเหล็ก (อ่านทุกครั้งก่อนเขียน test)

> **ห้ามใช้ InMemory DB เด็ดขาดในทุกกรณี**  
> ถ้าพบโค้ดที่ใช้ `UseInMemoryDatabase`, `Microsoft.EntityFrameworkCore.InMemory`, `TestDbContextFactory`, `TestDataSeeder`, หรือ `AppDbContext` ในไฟล์ `Services/` — ให้ลบออกและเขียนใหม่ด้วย Moq ทันที

- ทดสอบเฉพาะ **Service layer** — mock ทุก dependency (Repository, Mapper, UnitOfWork)
- ไม่มี DB จริง ไม่มี EF Core context ใน unit test
- ทุกอย่างสร้างใน `Arrange` ของ test นั้นเอง — ไม่มี shared seeder หรือ helper class
- Package ที่ใช้: `Moq` เท่านั้น (ไม่มี `Microsoft.EntityFrameworkCore.InMemory` ใน `.csproj`)

---

## มาตรฐาน (Quick Reference)

**โครงสร้างไฟล์**
```
Ecommerce.Tests/Services/
├── OrderServiceTests.cs         ← GetOrderById, CreateOrder, UpdateOrder, ConfirmOrder
├── OrderService_ApproveTests.cs ← ApproveOrders, SearchOrders
└── UserServiceTests.cs          ← Register, Login
```

**Naming**
```
Class   : {ServiceName}Tests
Method  : {MethodName}_{Scenario}_{ExpectedOutcome}
```
- Happy path → `ValidData`, `WithPhone`, `ConfirmedOrders`
- Error case → `UserNotFound`, `NotOwner`, `InsufficientStock`, `DuplicateEmail`

**AAA Pattern**
- `Arrange` — setup mock + สร้าง input
- `Act` — เรียก method เดียว
- `Assert` — assert สิ่งเดียว หรือใช้ `Assert.ThrowsAsync<T>` สำหรับ exception

**Mock patterns ที่ใช้บ่อย**
```csharp
// Return object
mock.Setup(r => r.GetByEmail("x")).ReturnsAsync(new User { ... });

// Return null
mock.Setup(r => r.GetByOrderId(99)).ReturnsAsync((Order?)null);

// Return Task (void)
mock.Setup(r => r.Create(It.IsAny<Order>())).Returns(Task.CompletedTask);

// ExecuteInTransactionAsync — ต้องเรียก callback จริง
unitOfWorkMock
    .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
    .Returns<Func<Task>>(fn => fn());

// Verify ถูกเรียก / ไม่ถูกเรียก
mock.Verify(r => r.Create(It.IsAny<Order>()), Times.Once);
mock.Verify(r => r.Update(It.IsAny<Order>()), Times.Never);
```

**Error ที่พบบ่อย**
- `Unsupported expression` → setup ครบทุก method ที่ service เรียก
- `Expected invocation... but was 0 times` → logic ใน service ไม่ได้เรียก method นั้น ตรวจ flow ใหม่

---

## Test Coverage Plan — OrderService

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

## Test Coverage Plan — UserService

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

## Implementation Phases

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
- [x] ลบไฟล์ทั้งหมดใน `Fakes/`
- [x] ลบไฟล์ทั้งหมดใน `Helpers/`
- [x] ลบ `UnitTest1.cs`
- [x] แทนที่ `Services/OrderServiceTests.cs` ด้วย skeleton ใหม่ (mock fields + constructor)
- [x] แทนที่ `Services/ApproveOrdersAsyncTests.cs` → rename เป็น `OrderService_ApproveTests.cs` พร้อม skeleton
- [x] แทนที่ `Services/UserServiceTests.cs` ด้วย skeleton ใหม่
- [x] `dotnet build` ผ่านไม่มี error

---

### Phase 1.5 — ลบ Repositories Tests และ InMemory Package

> เป้าหมาย: ไม่มี InMemory ในโปรเจกต์เลย, `dotnet build` ยังผ่าน

- [x] ลบ `Repositories/OrderRepositoryTests.cs` (integration test — ไม่อยู่ใน scope)
- [x] ลบ `Repositories/UserRepositoryTests.cs` (integration test — ไม่อยู่ใน scope)
- [x] ลบ `PackageReference` ของ `Microsoft.EntityFrameworkCore.InMemory` ออกจาก `.csproj`
- [x] `dotnet build` ผ่านไม่มี error

---

### Phase 2 — OrderServiceTests: `GetOrderByIdAsync` (4 tests)

- [x] `GetOrderByIdAsync_UserNotFound_ThrowsUnauthorizedAccessException` *(actual exception is UnauthorizedAccessException)*
- [x] `GetOrderByIdAsync_OrderNotFound_ThrowsKeyNotFoundException`
- [x] `GetOrderByIdAsync_NotOwner_ThrowsSecurityException`
- [x] `GetOrderByIdAsync_ValidOwner_ReturnsOrderResponseDto`
- [x] `dotnet test` → 4/4 pass

---

### Phase 3 — OrderServiceTests: `CreateOrderAsync` (4 tests)

- [x] `CreateOrderAsync_UserNotFound_ThrowsUnauthorizedAccessException` *(actual exception is UnauthorizedAccessException)*
- [x] `CreateOrderAsync_ProductNotActive_ThrowsInvalidOperationException`
- [x] `CreateOrderAsync_ValidData_ReturnsOrderResponseDto` (Status=Pending, TotalPrice ถูก)
- [x] `CreateOrderAsync_ValidData_CallsRepositoryCreateOnce` (Verify Times.Once)
- [x] `dotnet test` → 4/4 pass

---

### Phase 4 — OrderServiceTests: `UpdateOrderAsync` (6 tests)

- [x] `UpdateOrderAsync_UserNotFound_ThrowsUnauthorizedAccessException` *(actual exception is UnauthorizedAccessException)*
- [x] `UpdateOrderAsync_OrderNotFound_ThrowsKeyNotFoundException`
- [x] `UpdateOrderAsync_NotOwner_ThrowsSecurityException`
- [x] `UpdateOrderAsync_NotPending_ThrowsInvalidOperationException`
- [x] `UpdateOrderAsync_ProductNotFound_ThrowsInvalidOperationException`
- [x] `UpdateOrderAsync_ValidData_ReturnsOrderResponseDto`
- [x] `dotnet test` → 6/6 pass

---

### Phase 5 — OrderServiceTests: `ConfirmOrderAsync` (6 tests)

- [x] `ConfirmOrderAsync_UserNotFound_ThrowsUnauthorizedAccessException` *(actual exception is UnauthorizedAccessException)*
- [x] `ConfirmOrderAsync_OrderNotFound_ThrowsKeyNotFoundException`
- [x] `ConfirmOrderAsync_NotOwner_ThrowsSecurityException`
- [x] `ConfirmOrderAsync_NotPending_ThrowsInvalidOperationException`
- [x] `ConfirmOrderAsync_InsufficientStock_ThrowsInvalidOperationException`
- [x] `ConfirmOrderAsync_ValidData_StatusConfirmedAndReturnsDto`
- [x] `dotnet test` → 6/6 pass

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
