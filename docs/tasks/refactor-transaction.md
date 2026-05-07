# Refactor: Transaction ใน Service Layer สำหรับ UpdateOrderAsync

## ปัญหาปัจจุบัน

`UpdateOrderAsync` เรียก repository 2 ครั้ง และแต่ละ method จัดการ `SaveChangesAsync` เองแยกกัน:

```
RemoveItems → SaveChangesAsync  ← commit ที่ 1
Update      → SaveChangesAsync  ← commit ที่ 2
```

ไม่มี transaction ครอบ ถ้า `Update` ล้มเหลวหลัง `RemoveItems` สำเร็จ ข้อมูลจะอยู่ในสถานะที่ไม่สมบูรณ์ (items ถูกลบแล้วแต่ order ยังไม่ได้อัปเดต)

---

## แนวทางที่เลือก: IUnitOfWork

Service ไม่ควร inject `AppDbContext` โดยตรง (ละเมิด `Service → Repository → DbContext`)  
ให้สร้าง `IUnitOfWork` abstraction เพื่อให้ Service ควบคุม save และ transaction ได้โดยไม่รู้จัก EF Core

---

## ไฟล์ที่ต้องแก้ไขและสิ่งที่ต้องทำ

### 1. สร้าง `Data/IUnitOfWork.cs` (ไฟล์ใหม่) [x]

```csharp
namespace Data;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
}
```

> ใช้ `Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction`

---

### 2. สร้าง `Data/UnitOfWork.cs` (ไฟล์ใหม่) [x]

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    }
}
```

---

### 3. Register ใน `Program.cs` [x]

```csharp
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

### 4. แก้ `Repositories/Interfaces/IOrderRepository.cs` [x]

คง `Task` ไว้และ implementation จะ return `Task.CompletedTask` แทน `void`

> `Update` และ `RemoveItems` เปลี่ยนจาก `void` เป็น `Task` เพื่อให้ consistent กับ async interface และ caller สามารถ `await` ได้
> `Create` และ `UpdateRange` ยังคง `Task` ไว้ก่อน เพราะยังไม่ได้ refactor ใน iteration นี้

---

### 5. แก้ `Repositories/OrderRepository.cs` [x]

ลบ `SaveChangesAsync` ออกจาก `Update` และ `RemoveItems` และเปลี่ยน return type จาก `void` เป็น `Task` โดย return `Task.CompletedTask`:

```csharp
public Task Update(Order order)
{
    _context.Orders.Update(order);
    return Task.CompletedTask;
}

public Task RemoveItems(List<OrderItem> items)
{
    _context.Set<OrderItem>().RemoveRange(items);
    return Task.CompletedTask;
}
```

> Business logic ไม่เปลี่ยน — repository ยังคงทำหน้าที่ track changes ผ่าน EF Core เหมือนเดิม  
> เพียงแต่ไม่ persist เอง — `SaveChangesAsync` เป็น responsibility ของ Service แทน

---

### 6. แก้ `Services/OrderService.cs` [x]

Inject `IUnitOfWork` และห่อ `UpdateOrderAsync` ด้วย transaction:

#### Constructor

```csharp
private readonly IUnitOfWork _unitOfWork;

public OrderService(
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    IProductRepository productRepository,
    IMapper mapper,
    IUnitOfWork unitOfWork)
{
    _orderRepository = orderRepository;
    _userRepository = userRepository;
    _productRepository = productRepository;
    _mapper = mapper;
    _unitOfWork = unitOfWork;
}
```

#### UpdateOrderAsync (เฉพาะส่วน step 6–9) [x]

```csharp
// 6 delete old items and replace with new one
await using IDbContextTransaction tx = await _unitOfWork.BeginTransactionAsync();
try
{
        await _orderRepository.RemoveItems(order.Items);

            List<OrderItem> newItems = dto.Items.Select(item =>
            {
    decimal totalPrice = newItems.Sum(i => i.UnitPrice * i.Quantity);

    // 8 update order entity
    order.Items = newItems;
    order.TotalPrice = totalPrice;

    // 9 save changes to database (single round-trip)
    await _orderRepository.Update(order);
    await _unitOfWork.SaveChangesAsync();
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

---

## สรุปการเปลี่ยน Responsibility

| Layer      | ก่อน                                | หลัง                                      |
|------------|--------------------------------------|-------------------------------------------|
| Repository | Stage changes + SaveChangesAsync     | Stage changes เท่านั้น (no save)          |
| Service    | เรียก repo แล้วจบ                   | ควบคุม transaction boundary + SaveChanges |

---

## ขอบเขตของ Refactor นี้

- **เปลี่ยน**: `RemoveItems`, `Update` ใน `OrderRepository` และ `IOrderRepository`
- **เพิ่ม**: `IUnitOfWork`, `UnitOfWork`, inject ใน `OrderService`
- **ไม่เปลี่ยน**: `Create`, `UpdateRange`, `SearchOrders`, `GetByIds`, `GetByOrderId`
- **ไม่เปลี่ยน**: business logic ใด ๆ ใน service และ repository
