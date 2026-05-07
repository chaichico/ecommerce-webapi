# Fix: SqlServerRetryingExecutionStrategy กับ Manual Transaction

## ปัญหา

เมื่อเรียก `PUT /api/orders/{id}` จะได้รับ error 400:

```json
{
  "message": "The configured execution strategy 'SqlServerRetryingExecutionStrategy' does not support user-initiated transactions. Use the execution strategy returned by 'DbContext.Database.CreateExecutionStrategy()' to execute all the operations in the transaction as a retriable unit."
}
```

## สาเหตุ

`Program.cs` เปิดใช้ `EnableRetryOnFailure()` ซึ่งทำให้ EF Core ใช้ `SqlServerRetryingExecutionStrategy` โดยอัตโนมัติ

```csharp
sqlServerOptions.EnableRetryOnFailure(
    maxRetryCount: 5,
    maxRetryDelay: TimeSpan.FromSeconds(10),
    errorNumbersToAdd: null);
```

ขณะเดียวกัน `UpdateOrderAsync` ใน `OrderService.cs` เปิด manual transaction โดยตรง:

```csharp
await using IDbContextTransaction tx = await _unitOfWork.BeginTransactionAsync();
```

ทั้งสองอย่างนี้ conflict กัน เพราะ retry strategy ต้องควบคุม transaction เองทั้งหมด — ถ้า EF Core ต้อง retry กลางคัน แต่ transaction ถูกเปิดข้างนอกโดย user แล้ว มันจะไม่รู้ว่า state ปัจจุบันอยู่ที่ไหน จึง throw exception ทันที

## แนวทางแก้ไข

ใช้ `CreateExecutionStrategy()` เพื่อห่อ transaction ทั้งหมดไว้ใน retriable unit ตามที่ EF Core แนะนำ

### 1. เพิ่ม method ใน `IUnitOfWork`

```csharp
Task ExecuteInTransactionAsync(Func<Task> operation);
```

### 2. Implement ใน `UnitOfWork`

```csharp
public Task ExecuteInTransactionAsync(Func<Task> operation)
{
    return _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
    {
        await using IDbContextTransaction tx = await _context.Database.BeginTransactionAsync();
        try
        {
            await operation();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    });
}
```

### 3. อัปเดต `UpdateOrderAsync` ใน `OrderService`

แทนที่ block `await using IDbContextTransaction tx = ...` เดิม ด้วย:

```csharp
await _unitOfWork.ExecuteInTransactionAsync(async () =>
{
    // Remove old items
    await _orderRepository.RemoveItems(order.Items);

    // Create new items
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

    decimal totalPrice = newItems.Sum(i => i.UnitPrice * i.Quantity);

    order.Items = newItems;
    order.TotalPrice = totalPrice;

    await _orderRepository.Update(order);
    await _unitOfWork.SaveChangesAsync();
});
```

## ไฟล์ที่ต้องแก้

| ไฟล์ | สิ่งที่เปลี่ยน |
|------|--------------|
| `Data/IUnitOfWork.cs` | เพิ่ม `ExecuteInTransactionAsync` |
| `Data/UnitOfWork.cs` | Implement `ExecuteInTransactionAsync` โดยใช้ `CreateExecutionStrategy()` |
| `Services/OrderService.cs` | เปลี่ยน manual transaction ใน `UpdateOrderAsync` ให้ใช้ `ExecuteInTransactionAsync` แทน |

> `BeginTransactionAsync()` ใน `IUnitOfWork` / `UnitOfWork` ยังคงไว้ได้ แต่ไม่ควรเรียกใช้โดยตรงในขณะที่ retry strategy เปิดอยู่
