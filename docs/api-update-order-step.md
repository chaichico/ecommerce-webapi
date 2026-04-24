# User Update Order API - Step by Step Guide

คู่มือการเขียนโค้ด Update Order API แบบละเอียด

> **ต้องการ JWT Token** — endpoint นี้ต้องใช้ `[Authorize]` (Login ก่อนเสมอ)  
> **เงื่อนไข** — แก้ไขได้เฉพาะ Order ที่ Status เป็น `"Pending"` เท่านั้น

---

## 🎯 ลำดับการเขียนโค้ด

### 1️⃣ **Models/Dtos/UpdateOrderDto.cs** ← เริ่มที่นี่ก่อน

```csharp
using System.ComponentModel.DataAnnotations;

namespace Models.Dtos;

public class UpdateOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    public List<CreateOrderItemDto> Items { get; set; } = new();
}
```

**จุดสำคัญ:**
- โครงสร้างเหมือน `CreateOrderDto` แต่แยกเป็น class ใหม่เพื่อความชัดเจน
- ใช้ `CreateOrderItemDto` ซ้ำได้เลย (ProductId + Quantity) ไม่ต้องสร้างใหม่
- `Items` ที่ส่งมาจะ **แทนที่** items เดิมทั้งหมด

---

### 2️⃣ **Repositories/Interfaces/IOrderRepository.cs** — เพิ่ม method

```csharp
using Models;

namespace Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<Order> UpdateAsync(Order order);   // ← เพิ่มบรรทัดนี้
}
```

**จุดสำคัญ:**
- `UpdateAsync` รับ `Order` entity ที่แก้ไขแล้ว และ save ลง DB
- `GetByOrderNumberAsync` ที่มีอยู่แล้วใช้ดึง order ก่อน update ได้เลย

---

### 3️⃣ **Repositories/OrderRepository.cs** — implement UpdateAsync

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

    public async Task<Order> UpdateAsync(Order order)   // ← เพิ่ม method นี้
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }
}
```

**จุดสำคัญ:**
- `_context.Orders.Update(order)` — EF Core จะ track การเปลี่ยนแปลงทั้ง Order และ Items
- `SaveChangesAsync()` — บันทึกทุกอย่างในครั้งเดียว

---

### 4️⃣ **Services/Interfaces/IOrderService.cs** — เพิ่ม method

```csharp
using Models.Dtos;

namespace Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, string userEmail);
    Task<OrderResponseDto> UpdateOrderAsync(string orderNumber, UpdateOrderDto dto, string userEmail);   // ← เพิ่มบรรทัดนี้
}
```

**จุดสำคัญ:**
- รับ `orderNumber` จาก route parameter
- รับ `userEmail` จาก JWT claim ใน Controller (ป้องกันไม่ให้ user แก้ order คนอื่น)

---

### 5️⃣ **Services/OrderService.cs** — implement UpdateOrderAsync

```csharp
public async Task<OrderResponseDto> UpdateOrderAsync(string orderNumber, UpdateOrderDto dto, string userEmail)
{
    // 1. หา user จาก email (ดึงมาจาก JWT claim)
    User? user = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == userEmail);
    if (user == null)
    {
        throw new UnauthorizedAccessException("User not found");
    }

    // 2. หา order จาก OrderNumber พร้อม Include Items
    Order? order = await _orderRepository.GetByOrderNumberAsync(orderNumber);
    if (order == null)
    {
        throw new KeyNotFoundException("Order not found");
    }

    // 3. ตรวจสอบว่า order เป็นของ user คนนี้
    if (order.UserId != user.Id)
    {
        throw new UnauthorizedAccessException("You are not authorized to update this order");
    }

    // 4. ตรวจสอบว่า order ยัง Pending อยู่ (ถ้า Confirmed แล้วแก้ไม่ได้)
    if (order.Status != "Pending")
    {
        throw new InvalidOperationException("Only pending orders can be updated");
    }

    // 5. ตรวจสอบและดึงข้อมูล products ทุกตัวจาก DB
    List<int> productIds = dto.Items.Select(i => i.ProductId).ToList();
    List<Product> products = await _context.Products
        .Where(p => productIds.Contains(p.Id) && p.IsActive)
        .ToListAsync();

    if (products.Count != productIds.Count)
    {
        throw new Exception("One or more products not found or inactive");
    }

    // 6. ลบ items เดิมทั้งหมด แล้วใส่ items ใหม่แทน
    _context.Set<OrderItem>().RemoveRange(order.Items);

    List<OrderItem> newItems = dto.Items.Select(item =>
    {
        Product product = products.First(p => p.Id == item.ProductId);
        return new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            ProductName = product.ProductName,
            Quantity = item.Quantity,
            UnitPrice = product.Price
        };
    }).ToList();

    // 7. คำนวณ TotalPrice ใหม่
    decimal totalPrice = newItems.Sum(i => i.UnitPrice * i.Quantity);

    // 8. อัปเดต order entity
    order.Items = newItems;
    order.TotalPrice = totalPrice;

    // 9. บันทึกลง DB
    await _orderRepository.UpdateAsync(order);

    // 10. Return response
    return new OrderResponseDto
    {
        OrderNumber = order.OrderNumber,
        OrderDate = order.OrderDate,
        Status = order.Status,
        TotalPrice = order.TotalPrice,
        Items = newItems.Select(i => new OrderItemResponseDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            SubTotal = i.SubTotal
        }).ToList()
    };
}
```

**จุดสำคัญ:**
- ตรวจสอบ **ownership** — user ต้องเป็นเจ้าของ order นั้น (ป้องกันแก้ order คนอื่น)
- ตรวจสอบ **status** — แก้ไขได้เฉพาะ `"Pending"` เท่านั้น
- `RemoveRange(order.Items)` — ลบ items เดิมออกก่อน แล้วแทนที่ด้วย items ใหม่
- `UnitPrice` ดึงจาก product ใน DB เสมอ (ไม่รับจาก user)
- คำนวณ `TotalPrice` ใหม่ทุกครั้งหลัง items เปลี่ยน

---

### 6️⃣ **Controllers/OrdersController.cs** — เพิ่ม endpoint

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Dtos;
using Services.Interfaces;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
        string? userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { message = "Invalid token" });

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

    // PUT /api/orders/{orderNumber}   ← เพิ่ม endpoint นี้
    [HttpPut("{orderNumber}")]
    public async Task<IActionResult> UpdateOrder(string orderNumber, [FromBody] UpdateOrderDto dto)
    {
        string? userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            OrderResponseDto result = await _orderService.UpdateOrderAsync(orderNumber, dto, userEmail);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
```

**จุดสำคัญ:**
- Route: `PUT /api/orders/{orderNumber}` — ส่ง OrderNumber ผ่าน URL
- `[HttpPut("{orderNumber}")]` — รับ orderNumber จาก route parameter
- แยก exception แต่ละ type เพื่อ return HTTP status code ที่ถูกต้อง
  - `KeyNotFoundException` → `404 Not Found`
  - `UnauthorizedAccessException` → `401 Unauthorized`
  - `InvalidOperationException` → `400 Bad Request` (order ไม่ใช่ Pending)

---

## 📋 สรุปไฟล์ที่ต้องสร้าง/แก้ไข

| ไฟล์ | สร้างใหม่ / แก้ไข |
|------|------------------|
| `Models/Dtos/UpdateOrderDto.cs` | ✅ สร้างใหม่ |
| `Repositories/Interfaces/IOrderRepository.cs` | ✏️ แก้ไข (เพิ่ม `UpdateAsync`) |
| `Repositories/OrderRepository.cs` | ✏️ แก้ไข (implement `UpdateAsync`) |
| `Services/Interfaces/IOrderService.cs` | ✏️ แก้ไข (เพิ่ม `UpdateOrderAsync`) |
| `Services/OrderService.cs` | ✏️ แก้ไข (implement `UpdateOrderAsync`) |
| `Controllers/OrdersController.cs` | ✏️ แก้ไข (เพิ่ม `PUT /{orderNumber}`) |

> **หมายเหตุ:** `Program.cs` ไม่ต้องแก้ไข เพราะ `IOrderRepository` และ `IOrderService` ลง register ไว้แล้วตอน Create Order

---

## 🔐 วิธีทดสอบ API

### Request

```http
PUT /api/orders/ORD-20260424-A1B2C3D4
Authorization: Bearer <JWT_TOKEN_FROM_LOGIN>
Content-Type: application/json

{
  "items": [
    { "productId": 2, "quantity": 3 },
    { "productId": 4, "quantity": 1 }
  ]
}
```

### Response (200 OK) — สำเร็จ

```json
{
  "orderNumber": "ORD-20260424-A1B2C3D4",
  "orderDate": "2026-04-24T10:30:00Z",
  "status": "Pending",
  "totalPrice": 1500.00,
  "items": [
    {
      "productId": 2,
      "productName": "Laptop Stand",
      "quantity": 3,
      "unitPrice": 400.00,
      "subTotal": 1200.00
    },
    {
      "productId": 4,
      "productName": "USB Hub",
      "quantity": 1,
      "unitPrice": 300.00,
      "subTotal": 300.00
    }
  ]
}
```

### Error Responses

| สถานการณ์ | HTTP Status | Message |
|-----------|-------------|---------|
| ไม่มี JWT token | `401` | `"Invalid token"` |
| OrderNumber ไม่มีในระบบ | `404` | `"Order not found"` |
| Order เป็นของ user อื่น | `401` | `"You are not authorized to update this order"` |
| Order ไม่ใช่สถานะ Pending | `400` | `"Only pending orders can be updated"` |
| Product ไม่มีในระบบหรือ inactive | `400` | `"One or more products not found or inactive"` |

---

## ⚠️ Business Rules สำคัญ

1. **เฉพาะเจ้าของ** — user ต้องเป็นเจ้าของ order เท่านั้นถึงจะแก้ไขได้
2. **เฉพาะ Pending** — ถ้า order ถูก Confirm แล้ว (`"Confirmed"`) จะแก้ไขไม่ได้
3. **แทนที่ทั้งหมด** — items ใหม่ที่ส่งมาจะ **แทนที่** items เดิมทั้งหมด (ไม่ใช่ merge)
4. **UnitPrice จาก DB** — ราคาสินค้าดึงจาก DB เสมอ ไม่รับจาก request body
