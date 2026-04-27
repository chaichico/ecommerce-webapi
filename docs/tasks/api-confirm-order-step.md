# User Confirm Order API - Step by Step Guide

คู่มือการเขียนโค้ด Confirm Order API แบบละเอียด

> **ต้องการ JWT Token** — endpoint นี้ต้องใช้ `[Authorize]` (Login ก่อนเสมอ)  
> **เงื่อนไข** — ยืนยันได้เฉพาะ Order ที่ Status เป็น `"Pending"` เท่านั้น

---

## 🎯 ลำดับการเขียนโค้ด

### 1️⃣ **Models/Dtos/ConfirmOrderDto.cs** ← เริ่มที่นี่ก่อน

```csharp
using System.ComponentModel.DataAnnotations;

namespace Models.Dtos;

public class ConfirmOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Shipping address is required")]
    public string ShippingAddress { get; set; } = string.Empty;
}
```

**จุดสำคัญ:**
- มีแค่ `ShippingAddress` — field เดียวที่ user ต้องกรอกตอน Confirm
- `[Required]` บังคับให้กรอก ไม่สามารถ Confirm โดยไม่มีที่อยู่จัดส่ง

---

### 2️⃣ **Repositories/Interfaces/IOrderRepository.cs** — ตรวจสอบ (ไม่ต้องเพิ่ม method ใหม่)

```csharp
using Models;

namespace Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order?> GetByOrderIdAsync(int id);   // ← ใช้ method นี้
    Task<Order> UpdateAsync(Order order);     // ← ใช้ method นี้
}
```

**จุดสำคัญ:**
- `GetByOrderIdAsync` มีอยู่แล้ว — ใช้ค้นหา order ด้วย `id` (int)
- `UpdateAsync` มีอยู่แล้วตั้งแต่ Update Order step — ใช้บันทึก status และ address ใหม่
- **ไม่ต้องเพิ่ม method ใหม่** ใน Repository

---

### 3️⃣ **Services/Interfaces/IOrderService.cs** — เพิ่ม method

```csharp
using Models.Dtos;

namespace Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, string userEmail);
    Task<OrderResponseDto> UpdateOrderAsync(int id, UpdateOrderDto dto, string userEmail);
    Task<OrderResponseDto> ConfirmOrderAsync(int id, ConfirmOrderDto dto, string userEmail);   // ← เพิ่มบรรทัดนี้
}
```

**จุดสำคัญ:**
- รับ `id` (int) จาก route parameter
- รับ `userEmail` จาก JWT claim ใน Controller (ป้องกันไม่ให้ user Confirm order คนอื่น)
- รับ `ConfirmOrderDto` ที่มีแค่ `ShippingAddress`

---

### 4️⃣ **Services/OrderService.cs** — implement ConfirmOrderAsync

```csharp
public async Task<OrderResponseDto> ConfirmOrderAsync(int id, ConfirmOrderDto dto, string userEmail)
{
    // 1. หา user จาก email (ดึงมาจาก JWT claim)
    User? user = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == userEmail);
    if (user == null)
    {
        throw new UnauthorizedAccessException("User not found");
    }

    // 2. หา order จาก id พร้อม Include Items
    Order? order = await _orderRepository.GetByOrderIdAsync(id);
    if (order == null)
    {
        throw new KeyNotFoundException("Order not found");
    }

    // 3. ตรวจสอบว่า order เป็นของ user คนนี้
    if (order.UserId != user.Id)
    {
        throw new System.Security.SecurityException("You are not authorized to confirm this order");
    }

    // 4. ตรวจสอบว่า order ยัง Pending อยู่ (ถ้า Confirmed แล้ว confirm ซ้ำไม่ได้)
    if (order.Status != "Pending")
    {
        throw new InvalidOperationException("Only pending orders can be confirmed");
    }

    // 5. อัปเดต ShippingAddress และ Status
    order.ShippingAddress = dto.ShippingAddress;
    order.Status = "Confirmed";

    // 6. บันทึกลง DB
    await _orderRepository.UpdateAsync(order);

    // 7. Return response
    return new OrderResponseDto
    {
        OrderNumber = order.OrderNumber,
        OrderDate = order.OrderDate,
        Status = order.Status,
        TotalPrice = order.TotalPrice,
        Items = order.Items.Select(i => new OrderItemResponseDto
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
- ตรวจสอบ **ownership** — user ต้องเป็นเจ้าของ order เท่านั้นถึงจะ Confirm ได้
- ตรวจสอบ **status** — Confirm ได้เฉพาะ `"Pending"` เท่านั้น
- เปลี่ยน `Status` → `"Confirmed"` และบันทึก `ShippingAddress` ใหม่
- `GetByOrderIdAsync` จะ `Include(o => o.Items)` ไว้แล้ว ใช้ได้เลย

---

### 5️⃣ **Controllers/OrdersController.cs** — เพิ่ม endpoint

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
            return StatusCode(201, result);
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

    // PUT /api/orders/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        string? userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            OrderResponseDto result = await _orderService.UpdateOrderAsync(id, dto, userEmail);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.Security.SecurityException ex)
        {
            return StatusCode(403, new { message = ex.Message });
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

    // POST /api/orders/{id}/confirm   ← เพิ่ม endpoint นี้
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmOrder(int id, [FromBody] ConfirmOrderDto dto)
    {
        string? userEmail = User.FindFirst(ClaimTypes.Email)?.Value
                            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized(new { message = "Invalid token" });

        try
        {
            OrderResponseDto result = await _orderService.ConfirmOrderAsync(id, dto, userEmail);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (System.Security.SecurityException ex)
        {
            return StatusCode(403, new { message = ex.Message });
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
- Route: `POST /api/orders/{id}/confirm` — ส่ง Order `id` (int) ผ่าน URL
- `[HttpPost("{id}/confirm")]` — รับ id จาก route parameter
- แยก exception แต่ละ type เพื่อ return HTTP status code ที่ถูกต้อง
  - `KeyNotFoundException` → `404 Not Found`
  - `SecurityException` → `403 Forbidden`
  - `UnauthorizedAccessException` → `401 Unauthorized`
  - `InvalidOperationException` → `400 Bad Request` (order ไม่ใช่ Pending)

---

## 📋 สรุปไฟล์ที่ต้องสร้าง/แก้ไข

| ไฟล์ | สร้างใหม่ / แก้ไข |
|------|------------------|
| `Models/Dtos/ConfirmOrderDto.cs` | ✅ สร้างใหม่ |
| `Services/Interfaces/IOrderService.cs` | ✏️ แก้ไข (เพิ่ม `ConfirmOrderAsync`) |
| `Services/OrderService.cs` | ✏️ แก้ไข (implement `ConfirmOrderAsync`) |
| `Controllers/OrdersController.cs` | ✏️ แก้ไข (เพิ่ม `POST /{orderNumber}/confirm`) |

> **หมายเหตุ:** `IOrderRepository`, `OrderRepository`, และ `Program.cs` ไม่ต้องแก้ไข  
> เพราะ `GetByOrderIdAsync` และ `UpdateAsync` มีอยู่แล้วจาก Create/Update Order step

---

## 🔐 วิธีทดสอบ API

### Request

```http
POST /api/orders/1/confirm
Authorization: Bearer <JWT_TOKEN_FROM_LOGIN>
Content-Type: application/json

{
  "shippingAddress": "123 ถนนสุขุมวิท แขวงคลองเตย เขตคลองเตย กรุงเทพฯ 10110"
}
```

### Response (200 OK) — สำเร็จ

```json
{
  "orderNumber": "ORD-20260424-A1B2C3D4",
  "orderDate": "2026-04-24T10:30:00Z",
  "status": "Confirmed",
  "totalPrice": 1500.00,
  "items": [
    {
      "productId": 1,
      "productName": "Mechanical Keyboard",
      "quantity": 2,
      "unitPrice": 600.00,
      "subTotal": 1200.00
    },
    {
      "productId": 3,
      "productName": "Mouse Pad",
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
| Order id ไม่มีในระบบ | `404` | `"Order not found"` |
| Order เป็นของ user อื่น | `403` | `"You are not authorized to confirm this order"` |
| Order ไม่ใช่สถานะ Pending | `400` | `"Only pending orders can be confirmed"` |

---

## ⚠️ Business Rules สำคัญ

1. **เฉพาะเจ้าของ** — user ต้องเป็นเจ้าของ order เท่านั้นถึงจะ Confirm ได้
2. **เฉพาะ Pending** — ถ้า order ถูก Confirm แล้ว (`"Confirmed"`) จะ Confirm ซ้ำไม่ได้
3. **ShippingAddress บังคับ** — ต้องกรอกที่อยู่จัดส่งก่อนถึงจะ Confirm ได้
4. **Status เปลี่ยนถาวร** — เมื่อ Confirm แล้ว status จะเป็น `"Confirmed"` และแก้ไข items ไม่ได้อีก
