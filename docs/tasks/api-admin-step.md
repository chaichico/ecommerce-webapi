# Admin APIs - Step by Step Guide

คู่มือการเขียนโค้ด Admin APIs แบบละเอียด

> **ต้องการ Basic Authentication** — endpoints ทั้งหมดใน Admin ต้องใช้ Basic Auth  
> **Credentials** — กำหนด Username/Password ใน `appsettings.json` / ENV เท่านั้น ห้าม Hardcode

---

## 🎯 ลำดับการเขียนโค้ด

### 1️⃣ **appsettings.json** — เพิ่ม Admin credentials

```json
{
  "AdminAuth": {
    "Username": "admin",
    "Password": "admin1234"
  }
}
```

**จุดสำคัญ:**
- กำหนดค่าใน `appsettings.json` หรือ Environment Variables ในการ deploy จริง
- **ห้าม Hardcode** credentials ใน source code

---

### 2️⃣ **Models/Dtos/AdminOrderResponseDto.cs** ← สร้างใหม่

```csharp
namespace Models.Dtos;

public class AdminOrderResponseDto
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public AdminUserInfoDto User { get; set; } = null!;
    public List<OrderItemResponseDto> Items { get; set; } = new();
}

public class AdminUserInfoDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

**จุดสำคัญ:**
- Response แสดง User info (ชื่อ, นามสกุล, Email) ด้วย เพื่อให้ Admin เห็นว่าใครสั่ง
- `ShippingAddress` แสดงใน Admin view (User ปกติจะไม่แสดง)
- `OrderItemResponseDto` ใช้ร่วมกับ User API ได้เลย (มีอยู่แล้ว)

---

### 3️⃣ **Models/Dtos/ApproveOrdersDto.cs** ← สร้างใหม่

```csharp
using System.ComponentModel.DataAnnotations;

namespace Models.Dtos;

public class ApproveOrdersDto
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one order id is required")]
    public List<int> OrderIds { get; set; } = new();
}
```

**จุดสำคัญ:**
- รับ `OrderIds` เป็น `List<int>` เพื่อให้ Admin Approve หลาย Order พร้อมกันได้ — ใช้ id เดียวกับ User API
- `[Required]` + `[MinLength(1)]` ป้องกันการส่ง list ว่างมา

---

### 4️⃣ **Repositories/Interfaces/IOrderRepository.cs** — เพิ่ม methods

```csharp
using Models;

namespace Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);
    Task<Order?> GetByOrderIdAsync(int id);
    Task<Order> UpdateAsync(Order order);
    Task<List<Order>> SearchOrdersAsync(string? orderNumber, string? firstName, string? lastName);  // ← เพิ่ม
    Task<List<Order>> GetByIdsAsync(List<int> ids);            // ← เพิ่ม
    Task UpdateRangeAsync(List<Order> orders);                 // ← เพิ่ม
}
```

**จุดสำคัญ:**
- `SearchOrdersAsync` — ค้นหาด้วย `orderNumber`, `firstName`, `lastName` (ทุก parameter เป็น optional — ส่งมาตัวไหนก็ filter เฉพาะตัวนั้น)
- `GetByIdsAsync` — ดึง orders หลายรายการพร้อมกันสำหรับ Approve ด้วย id (consistent กับ User API)
- `UpdateRangeAsync` — บันทึก orders หลายรายการพร้อมกันใน batch เดียว

---

### 5️⃣ **Repositories/OrderRepository.cs** — implement methods ใหม่

```csharp
public async Task<List<Order>> SearchOrdersAsync(string? orderNumber, string? firstName, string? lastName)
{
    IQueryable<Order> query = _context.Orders
        .Include(o => o.Items)
        .Include(o => o.User);

    if (!string.IsNullOrWhiteSpace(orderNumber))
        query = query.Where(o => o.OrderNumber.Contains(orderNumber));

    if (!string.IsNullOrWhiteSpace(firstName))
        query = query.Where(o => o.User.FirstName.Contains(firstName));

    if (!string.IsNullOrWhiteSpace(lastName))
        query = query.Where(o => o.User.LastName.Contains(lastName));

    return await query.ToListAsync();
}

public async Task<List<Order>> GetByIdsAsync(List<int> ids)
{
    return await _context.Orders
        .Include(o => o.Items)
        .Include(o => o.User)
        .Where(o => ids.Contains(o.Id))
        .ToListAsync();
}

public async Task UpdateRangeAsync(List<Order> orders)
{
    _context.Orders.UpdateRange(orders);
    await _context.SaveChangesAsync();
}
```

**จุดสำคัญ:**
- ใช้ `IQueryable<Order>` เพื่อ build query แบบ conditional — filter เฉพาะ parameter ที่ส่งมา
- `Include(o => o.User)` — ต้อง Include เพื่อ filter ตาม FirstName/LastName และ map ใน response
- `UpdateRange` — EF Core batch update ในคำสั่งเดียว

---

### 6️⃣ **Services/Interfaces/IOrderService.cs** — เพิ่ม methods

```csharp
using Models.Dtos;

namespace Services.Interfaces;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto dto, string userEmail);
    Task<OrderResponseDto> UpdateOrderAsync(int id, UpdateOrderDto dto, string userEmail);
    Task<OrderResponseDto> ConfirmOrderAsync(int id, ConfirmOrderDto dto, string userEmail);
    Task<List<AdminOrderResponseDto>> SearchOrdersAsync(string? orderNumber, string? firstName, string? lastName);  // ← เพิ่ม
    Task<List<AdminOrderResponseDto>> ApproveOrdersAsync(ApproveOrdersDto dto);  // ← เพิ่ม
}
```

---

### 7️⃣ **Services/OrderService.cs** — implement methods ใหม่

```csharp
public async Task<List<AdminOrderResponseDto>> SearchOrdersAsync(string? orderNumber, string? firstName, string? lastName)
{
    List<Order> orders = await _orderRepository.SearchOrdersAsync(orderNumber, firstName, lastName);

    return orders.Select(o => new AdminOrderResponseDto
    {
        OrderNumber = o.OrderNumber,
        OrderDate = o.OrderDate,
        Status = o.Status,
        TotalPrice = o.TotalPrice,
        ShippingAddress = o.ShippingAddress,
        User = new AdminUserInfoDto
        {
            FirstName = o.User.FirstName,
            LastName = o.User.LastName,
            Email = o.User.Email
        },
        Items = o.Items.Select(i => new OrderItemResponseDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            SubTotal = i.SubTotal
        }).ToList()
    }).ToList();
}

public async Task<List<AdminOrderResponseDto>> ApproveOrdersAsync(ApproveOrdersDto dto)
{
    // 1. ดึง orders ตาม OrderIds ที่ส่งมา
    List<Order> orders = await _orderRepository.GetByIdsAsync(dto.OrderIds);

    // 2. ตรวจสอบว่าพบ orders ทั้งหมดไหม
    if (orders.Count != dto.OrderIds.Count)
    {
        List<int> foundIds = orders.Select(o => o.Id).ToList();
        List<int> notFound = dto.OrderIds.Except(foundIds).ToList();
        throw new KeyNotFoundException($"Orders not found: {string.Join(", ", notFound)}");
    }

    // 3. เปลี่ยน Status เป็น Confirmed เฉพาะที่ยัง Pending อยู่
    foreach (Order order in orders)
    {
        if (order.Status == "Pending")
        {
            order.Status = "Confirmed";
        }
    }

    // 4. บันทึกทั้งหมดใน batch เดียว
    await _orderRepository.UpdateRangeAsync(orders);

    // 5. Return response
    return orders.Select(o => new AdminOrderResponseDto
    {
        OrderNumber = o.OrderNumber,
        OrderDate = o.OrderDate,
        Status = o.Status,
        TotalPrice = o.TotalPrice,
        ShippingAddress = o.ShippingAddress,
        User = new AdminUserInfoDto
        {
            FirstName = o.User.FirstName,
            LastName = o.User.LastName,
            Email = o.User.Email
        },
        Items = o.Items.Select(i => new OrderItemResponseDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            SubTotal = i.SubTotal
        }).ToList()
    }).ToList();
}
```

**จุดสำคัญ:**
- `ApproveOrdersAsync` ตรวจสอบว่า Order Id ทุกตัวมีอยู่จริง — ถ้าไม่พบบางตัวจะ throw `KeyNotFoundException`
- Approve เฉพาะ Order ที่ Status เป็น `"Pending"` — ถ้าเป็น `"Confirmed"` อยู่แล้วจะข้ามไป (ไม่ throw error)
- `UpdateRangeAsync` — บันทึกทุก order ใน transaction เดียว

---

### 8️⃣ **Controllers/AdminController.cs** ← สร้างใหม่

```csharp
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Models.Dtos;
using Services.Interfaces;

namespace Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IConfiguration _configuration;

    public AdminController(IOrderService orderService, IConfiguration configuration)
    {
        _orderService = orderService;
        _configuration = configuration;
    }

    // ── Basic Auth helper ──────────────────────────────────────────────────────
    private bool IsAuthorized()
    {
        string? authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
            return false;

        string base64Credentials = authHeader["Basic ".Length..].Trim();
        string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
        string[] parts = credentials.Split(':', 2);
        if (parts.Length != 2)
            return false;

        string username = _configuration["AdminAuth:Username"] ?? string.Empty;
        string password = _configuration["AdminAuth:Password"] ?? string.Empty;

        return parts[0] == username && parts[1] == password;
    }

    // GET /api/admin/orders?orderNumber=&firstName=&lastName=
    [HttpGet("orders")]
    public async Task<IActionResult> SearchOrders(
        [FromQuery] string? orderNumber,
        [FromQuery] string? firstName,
        [FromQuery] string? lastName)
    {
        if (!IsAuthorized())
            return Unauthorized(new { message = "Invalid credentials" });

        try
        {
            List<AdminOrderResponseDto> result = await _orderService.SearchOrdersAsync(orderNumber, firstName, lastName);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /api/admin/orders/approve
    [HttpPost("orders/approve")]
    public async Task<IActionResult> ApproveOrders([FromBody] ApproveOrdersDto dto)
    {
        if (!IsAuthorized())
            return Unauthorized(new { message = "Invalid credentials" });

        try
        {
            List<AdminOrderResponseDto> result = await _orderService.ApproveOrdersAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
```

**จุดสำคัญ:**
- ใช้ `IsAuthorized()` helper แทน `[Authorize]` attribute เพราะ Basic Auth ต้อง validate เอง
- อ่าน credentials จาก `IConfiguration` — ดึงมาจาก `appsettings.json` หรือ ENV
- Decode Base64 → split `:` เพื่อแยก username/password อย่างปลอดภัย
- `credentials.Split(':', 2)` — `maxCount: 2` ป้องกันกรณี password มี `:` อยู่ด้วย

---

## 📋 สรุปไฟล์ที่ต้องสร้าง/แก้ไข

| ไฟล์ | สร้างใหม่ / แก้ไข |
|------|------------------|
| `appsettings.json` | ✏️ แก้ไข (เพิ่ม `AdminAuth` section) |
| `Models/Dtos/AdminOrderResponseDto.cs` | ✅ สร้างใหม่ |
| `Models/Dtos/ApproveOrdersDto.cs` | ✅ สร้างใหม่ |
| `Repositories/Interfaces/IOrderRepository.cs` | ✏️ แก้ไข (เพิ่ม 3 methods) |
| `Repositories/OrderRepository.cs` | ✏️ แก้ไข (implement 3 methods) |
| `Services/Interfaces/IOrderService.cs` | ✏️ แก้ไข (เพิ่ม 2 methods) |
| `Services/OrderService.cs` | ✏️ แก้ไข (implement 2 methods) |
| `Controllers/AdminController.cs` | ✅ สร้างใหม่ |

> **หมายเหตุ:** `Program.cs` ไม่ต้องแก้ไข เพราะ `IOrderService` และ `IOrderRepository` ถูก register ไว้แล้ว

---

## 🔐 วิธีทดสอบ API

### Search Orders

```http
GET /api/admin/orders?firstName=สมชาย
Authorization: Basic YWRtaW46YWRtaW4xMjM0
```

> `YWRtaW46YWRtaW4xMjM0` = Base64 encode ของ `admin:admin1234`

### Response (200 OK)

```json
[
  {
    "orderNumber": "ORD-20260424-A1B2C3D4",
    "orderDate": "2026-04-24T10:30:00Z",
    "status": "Pending",
    "totalPrice": 1500.00,
    "shippingAddress": "123 ถนนสุขุมวิท กรุงเทพฯ",
    "user": {
      "firstName": "สมชาย",
      "lastName": "ใจดี",
      "email": "somchai@example.com"
    },
    "items": [
      {
        "productId": 1,
        "productName": "Mechanical Keyboard",
        "quantity": 2,
        "unitPrice": 600.00,
        "subTotal": 1200.00
      }
    ]
  }
]
```

---

### Approve Orders

```http
POST /api/admin/orders/approve
Authorization: Basic YWRtaW46YWRtaW4xMjM0
Content-Type: application/json

{
  "orderIds": [1, 2]
}
```

### Response (200 OK) — สำเร็จ

```json
[
  {
    "orderNumber": "ORD-20260424-A1B2C3D4",
    "status": "Confirmed",
    "totalPrice": 1500.00,
    ...
  },
  {
    "orderNumber": "ORD-20260424-B2C3D4E5",
    "status": "Confirmed",
    "totalPrice": 800.00,
    ...
  }
]
```

### Response (401 Unauthorized) — credentials ผิด

```json
{ "message": "Invalid credentials" }
```

### Response (404 Not Found) — Order Id ไม่พบ

```json
{ "message": "Orders not found: 99" }
```
