# Refactor: Add Transaction to `UpdateOrderAsync`

## ปัญหา

`UpdateOrderAsync` มี 2 database operations แยกกัน:

```
RemoveItems(order.Items)  →  SaveChangesAsync() ← commit #1
Update(order)             →  SaveChangesAsync() ← commit #2
```

ถ้า commit #2 fail → items ถูกลบไปแล้วแต่ items ใหม่ไม่ถูกสร้าง → order สูญหาย items ถาวร

---

## แนวทางแก้ไข

### วิธีที่เลือก: Transaction ผ่าน `IDbContextTransaction` ใน Repository Layer

ใช้ `AppDbContext.Database.BeginTransactionAsync()` ครอบทั้ง 2 operations ให้เป็น atomic unit เดียว

---

## ขั้นตอนการแก้ไข

### Step 1 — เพิ่ม method `UpdateOrderWithItemsAsync` ใน `IOrderRepository`

```csharp
Task UpdateOrderWithItemsAsync(Order order, List<OrderItem> oldItems, List<OrderItem> newItems);
```

method นี้รับผิดชอบ:
1. Begin transaction
2. RemoveRange(oldItems) + SaveChanges
3. order.Items = newItems + Update(order) + SaveChanges
4. Commit
5. Rollback อัตโนมัติถ้า throw

### Step 2 — Implement ใน `OrderRepository`

```csharp
public async Task UpdateOrderWithItemsAsync(Order order, List<OrderItem> oldItems, List<OrderItem> newItems)
{
    using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
        await _context.Database.BeginTransactionAsync();

    _context.Set<OrderItem>().RemoveRange(oldItems);
    await _context.SaveChangesAsync();

    order.Items = newItems;
    _context.Orders.Update(order);
    await _context.SaveChangesAsync();

    await transaction.CommitAsync();
}
```

> ถ้า SaveChangesAsync() ตัวที่ 2 throw → transaction ถูก rollback อัตโนมัติเมื่อ `using` scope จบ

### Step 3 — แก้ `UpdateOrderAsync` ใน `OrderService`

แทนที่ 2 calls แยก:

```csharp
// เดิม (ลบออก)
await _orderRepository.RemoveItems(order.Items);
// ... build newItems ...
await _orderRepository.Update(order);
```

ด้วย 1 call:

```csharp
// ใหม่
await _orderRepository.UpdateOrderWithItemsAsync(order, order.Items, newItems);
```

### Step 4 — ลบ `RemoveItems` ออกจาก Interface และ Repository (ถ้าไม่มีที่อื่นใช้)

ตรวจสอบก่อนว่า `RemoveItems` ถูกใช้ที่อื่นนอกจาก `UpdateOrderAsync` ไหม ถ้าไม่มี ให้ลบทิ้งเพื่อ clean interface

---

## Files ที่ต้องแก้

| File | การเปลี่ยนแปลง |
|------|----------------|
| `Repositories/Interfaces/IOrderRepository.cs` | เพิ่ม `UpdateOrderWithItemsAsync`, พิจารณาลบ `RemoveItems` |
| `Repositories/OrderRepository.cs` | Implement `UpdateOrderWithItemsAsync` พร้อม transaction |
| `Services/OrderService.cs` | แทนที่ 2 calls ด้วย `UpdateOrderWithItemsAsync` ใน `UpdateOrderAsync` |

---

## สิ่งที่ไม่ต้องแก้

- `ConfirmOrderAsync` — stock + order status ถูก track โดย EF Change Tracker เดียวกัน commit ครั้งเดียวผ่าน `_orderRepository.Update(order)` อยู่แล้ว
- `ApproveOrdersAsync` — มีแค่ `UpdateRange` ครั้งเดียว

---

## ข้อควรระวัง

- Transaction เปิด connection ค้างไว้ — method นี้ควร complete เร็ว ไม่ทำ I/O อื่นระหว่าง transaction
- ไม่ต้อง inject `AppDbContext` เข้า `OrderService` โดยตรง — การ implement transaction ควรอยู่ใน Repository Layer เพื่อรักษา dependency direction
