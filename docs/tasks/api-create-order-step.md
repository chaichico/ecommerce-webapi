# User Create Order API - Step by Step Guide

คู่มือการเขียนโค้ด Create Order API แบบละเอียด

> **ต้องการ JWT Token** — endpoint นี้ต้องใช้ `[Authorize]` (Login ก่อนเสมอ)

---

## 🎯 ลำดับการเขียนโค้ด

### 1️⃣ **Models/Dtos/CreateOrderItemDto.cs** ← เริ่มที่นี่ก่อน

```csharp
using System.ComponentModel.DataAnnotations;

namespace Models.Dtos;

public class CreateOrderItemDto
{
    [Required]
    public int ProductId { get; set; }      // Product Number

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}
```

**จุดสำคัญ:**
- `ProductId` คือ Product Number ที่ใช้ระบุสินค้า
- `Quantity` ต้องมากกว่า 0 เสมอ

---

### 2️⃣ **Models/Dtos/CreateOrderDto.cs**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Models.Dtos;

public class CreateOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}
```

**จุดสำคัญ:**
- รับ list ของ items เพราะ 1 order มีหลาย product ได้
- `[MinLength(1)]` บังคับให้มีสินค้าอย่างน้อย 1 ชิ้น

---

### 3️⃣ **Models/Dtos/OrderResponseDto.cs**

```csharp
namespace Models.Dtos;

public class OrderResponseDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class OrderItemResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}
```

**จุดสำคัญ:**
- Return `OrderNumber` เป็น main identifier
- Return รายละเอียด items พร้อม SubTotal แต่ละรายการ
- Return `TotalPrice` รวมทั้งหมด

---

### 4️⃣ **Repositories/Interfaces/IOrderRepository.cs**

```csharp
using Models;

namespace Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
}
```

**จุดสำคัญ:**
- `CreateAsync` — บันทึก order ใหม่พร้อม items ลง DB
- `GetByOrderNumberAsync` — ใช้สำหรับค้นหา order ด้วย OrderNumber (จะใช้ใน Update/Confirm)

---

### 5️⃣ **Repositories/OrderRepository.cs**

```csharp
using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Repositories.Interfaces;

namespace Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }
}
```

**จุดสำคัญ:**
- `Include(o => o.Items)` — load OrderItems มาด้วยพร้อม Order
- `SaveChangesAsync()` — บันทึกทั้ง Order และ OrderItems ในครั้งเดียว (EF Core cascade)
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข
ขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขขข

---

### 6️⃣ **Services/Interfaces/IOrderService.cs**

```csharp
using Models.Dtos;

namespace Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, string userEmail);
}
```

**จุดสำคัญ:**
- รับ `userEmail` แยกต่างหากจาก DTO เพราะดึงมาจาก JWT token ใน Controller
- ไม่ให้ user ส่ง userId เองใน request body (security)

---

### 7️⃣ **Services/OrderService.cs**

```csharp
using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Dtos;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly AppDbContext _context;

    public OrderService(IOrderRepository orderRepository, AppDbContext context)
    {
        _orderRepository = orderRepository;
        _context = context;
    }

    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, string userEmail)
    {
        // 1. หา user จาก email (ดึงมาจาก JWT claim)
        User? user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == userEmail);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // 2. ตรวจสอบและดึงข้อมูล products ทุกตัวจาก DB
        List<int> productIds = dto.Items.Select(i => i.ProductId).ToList();
        List<Product> products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        if (products.Count != productIds.Count)
        {
            throw new Exception("One or more products not found or inactive");
        }

        // 3. สร้าง OrderNumber แบบ unique (เช่น ORD-20260424-XXXX)
        string orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        // 4. สร้าง OrderItems พร้อม UnitPrice จาก Product จริง
        List<OrderItem> orderItems = dto.Items.Select(item =>
        {
            Product product = products.First(p => p.Id == item.ProductId);
            return new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.ProductName,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };
        }).ToList();

        // 5. คำนวณ TotalPrice
        decimal totalPrice = orderItems.Sum(i => i.UnitPrice * i.Quantity);

        // 6. สร้าง Order entity
        Order order = new Order
        {
            OrderNumber = orderNumber,
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
            ShippingAddress = string.Empty,   // จะกรอกตอน Confirm Order
            UserId = user.Id,
            Items = orderItems,
            TotalPrice = totalPrice
        };

        // 7. บันทึกลง DB
        await _orderRepository.CreateAsync(order);

        // 8. Return response
        return new OrderResponseDto
        {
            OrderNumber = order.OrderNumber,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            Items = orderItems.Select(i => new OrderItemResponseDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal
            }).ToList()
        };
    }
}
```

**จุดสำคัญ:**
- `userEmail` มาจาก JWT claim — ไม่รับจาก request body
- ตรวจสอบว่า product มีอยู่จริงและ `IsActive = true` ก่อนสร้าง order
- `UnitPrice` ดึงจาก product จริงใน DB (ป้องกัน user ส่งราคาเองมา)
- `ShippingAddress` เป็น empty ก่อน — จะกรอกตอน Confirm Order
- `OrderNumber` สร้างแบบ unique ด้วย format `ORD-{date}-{guid}`

---

### 8️⃣ **Controllers/OrdersController.cs** ← Controller ใหม่

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Dtos;
using Services.Interfaces;
using System.Security.Claims;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]   // ← ทุก endpoint ใน controller นี้ต้องการ JWT
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // POST /api/orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        // ดึง email จาก JWT claim
        string? userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                         ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userEmail))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        try
        {
            OrderResponseDto result = await _orderService.CreateOrderAsync(dto, userEmail);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
```

**จุดสำคัญ:**
- `[Authorize]` ที่ระดับ class — ทุก endpoint ต้องส่ง JWT header
- `User.FindFirst(ClaimTypes.Email)` — ดึง email จาก JWT token ที่ login ไว้
- Route: `POST /api/orders`
- Return `Ok(result)` พร้อม OrderNumber และ items

---

### 9️⃣ **Program.cs** - Register IOrderRepository และ IOrderService

```csharp
// Register services (เพิ่มบรรทัดเหล่านี้ต่อจาก IUserService)
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

**จุดสำคัญ:**
- เพิ่ม 2 บรรทัดนี้ในส่วน Register services ใน `Program.cs`
- ต้อง register ทั้ง Repository และ Service

---

## 📋 สรุปไฟล์ที่ต้องสร้าง/แก้ไข

| ไฟล์ | สร้างใหม่ / แก้ไข |
|------|------------------|
| `Models/Dtos/CreateOrderItemDto.cs` | ✅ สร้างใหม่ |
| `Models/Dtos/CreateOrderDto.cs` | ✅ สร้างใหม่ |
| `Models/Dtos/OrderResponseDto.cs` | ✅ สร้างใหม่ |
| `Repositories/Interfaces/IOrderRepository.cs` | ✅ สร้างใหม่ |
| `Repositories/OrderRepository.cs` | ✅ สร้างใหม่ |
| `Services/Interfaces/IOrderService.cs` | ✅ สร้างใหม่ |
| `Services/OrderService.cs` | ✅ สร้างใหม่ |
| `Controllers/OrdersController.cs` | ✅ สร้างใหม่ |
| `Program.cs` | ✏️ แก้ไข (เพิ่ม 2 บรรทัด) |

---

## 🔐 วิธีทดสอบ API

### Request

```http
POST /api/orders
Authorization: Bearer <JWT_TOKEN_FROM_LOGIN>
Content-Type: application/json

{
  "items": [
    { "productId": 1, "quantity": 2 },
    { "productId": 3, "quantity": 1 }
  ]
}
```

### Response (200 OK)

```json
{
  "orderNumber": "ORD-20260424-A1B2C3D4",
  "orderDate": "2026-04-24T10:30:00Z",
  "status": "Pending",
  "totalPrice": 299.97,
  "items": [
    {
      "productId": 1,
      "productName": "Product A",
      "quantity": 2,
      "unitPrice": 99.99,
      "subTotal": 199.98
    },
    {
      "productId": 3,
      "productName": "Product C",
      "quantity": 1,
      "unitPrice": 99.99,
      "subTotal": 99.99
    }
  ]
}
```

### Error Responses

| Status | เมื่อไหร่ |
|--------|----------|
| `401 Unauthorized` | ไม่ส่ง JWT token หรือ token หมดอายุ |
| `400 Bad Request` | ProductId ไม่มีในระบบหรือ inactive |
| `400 Bad Request` | Items เป็น empty list |
