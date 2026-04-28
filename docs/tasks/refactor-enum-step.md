# Refactor OrderStatus to C# Enum — Complete Guide

สำหรับแปลง OrderStatus จากการใช้ static class ที่มี string constants เป็น C# enum ที่แท้จริง

---

## 📋 ขั้นตอนทั้งหมด

### Step 1: สร้าง OrderStatus Enum ใน `Models/Enums/OrderStatus.cs`

```csharp
namespace Models.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Approved = 2
}
```

**ทำไมต้องมีค่า int?**
- EF Core จะเก็บเป็น `int` ในฐานข้อมูลโดยค่าเริ่มต้น
- ช่วยให้ database storage efficient

---

### Step 2: Update `Models/Order.cs` — เปลี่ยน Status property type

**Before:**
```csharp
using Models;

public class Order
{
    public int Id {get; set;}
    public string OrderNumber {get; set;} = string.Empty;
    public DateTime OrderDate {get; set;} = DateTime.UtcNow;
    
    [Required]
    public string Status {get; set;} = OrderStatus.Pending;  // ❌ string
```

**After:**
```csharp
using Models;
using Models.Enums;

public class Order
{
    public int Id {get; set;}
    public string OrderNumber {get; set;} = string.Empty;
    public DateTime OrderDate {get; set;} = DateTime.UtcNow;
    
    [Required]
    public OrderStatus Status {get; set;} = OrderStatus.Pending;  // ✅ enum
```

---

### Step 3: Update `Data/Configurations/OrderConfiguration.cs`

**Before:**
```csharp
builder.Property(o => o.Status)
    .IsRequired()
    .HasDefaultValue(OrderStatus.Pending);
```

**After:**
```csharp
using Models.Enums;

builder.Property(o => o.Status)
    .IsRequired()
    .HasDefaultValue(OrderStatus.Pending);  // Default value ให้เป็น enum
```

---

### Step 4: Update `Data/DbSeeder.cs` — Seed data ด้วย enum values

**Before:**
```csharp
Status = OrderStatus.Pending,  // ❌ string constant
```

**After:**
```csharp
Status = OrderStatus.Pending,  // ✅ enum value
```

*Note: Code นี้จะยังใช้ได้เพราะ enum value ชื่อเดิม ถ้ามี static class ก่อนหน้านี้ให้ลบออก*

---

### Step 5: Update `Services/OrderService.cs` — เปลี่ยนการเปรียบเทียบ enum

ทั่วทั้ง OrderService ให้ลบ `using Models;` ที่เก่า และเพิ่ม:

```csharp
using Models.Enums;
```

**Comparisons จะยังทำงานเหมือนเดิม:**
```csharp
if (order.Status != OrderStatus.Pending)  // ✅ ยังทำงาน

order.Status = OrderStatus.Confirmed;     // ✅ ยังทำงาน
```

---

### Step 6: Update DTOs (ถ้ามี) — `Models/Dtos/`

ถ้า DTOs มี Status property:

**Before:**
```csharp
public string Status { get; set; }  // ❌ string
```

**After:**
```csharp
using Models.Enums;

public OrderStatus Status { get; set; }  // ✅ enum
```

ถ้า DTO ต้องการ return status เป็น string (JSON serialization):
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public OrderStatus Status { get; set; }
```

---

### Step 7: ลบ OrderStatus static class เก่า

ถ้าเดิมมี `Models/OrderStatus.cs` ที่เป็น static class ให้ **ลบออก** เลย ไม่ต้องมีอีกต่อไป

---

### Step 8: สร้าง Migration ใหม่

```bash
dotnet ef migrations add ConvertOrderStatusToEnum
```

**Migration จะ:**
- เปลี่ยน column type จาก `nvarchar` เป็น `int`
- Set default value เป็น 0 (Pending)
- Migrate existing data: `"Pending"` → 0, `"Confirmed"` → 1, `"Approved"` → 2

---

### Step 9: Update Database

```bash
dotnet ef database update
```

---

### Step 10: Test ทั้งหมด

Run unit tests:
```bash
dotnet test
```

ตรวจสอบ:
- ✅ Order สามารถสร้างด้วย enum status ได้
- ✅ Query orders โดย enum status ทำงาน
- ✅ Update order status ทำงาน
- ✅ DTOs serialization/deserialization ถูกต้อง

---

## 🔍 Checklist ทั้งหมด

- [ ] สร้าง `Models/Enums/OrderStatus.cs` (enum)
- [ ] Update `Models/Order.cs` — เปลี่ยน Status type
- [ ] Update `Models/Enums/OrderStatus.cs` (ชื่อ namespace ถ้าต้อง)
- [ ] Update `Data/Configurations/OrderConfiguration.cs`
- [ ] Update `Data/DbSeeder.cs`
- [ ] Update `Services/OrderService.cs` — add using Models.Enums
- [ ] Update all DTOs ที่มี Status
- [ ] ลบ `Models/OrderStatus.cs` เก่า (static class)
- [ ] สร้าง migration: `dotnet ef migrations add ConvertOrderStatusToEnum`
- [ ] Update database: `dotnet ef database update`
- [ ] Run tests: `dotnet test`
- [ ] ตรวจสอบ API response เป็น enum / string ที่ต้องการ

---

## ⚠️ สิ่งที่ต้องระวัง

1. **Namespace** — Enum อยู่ใน `Models.Enums` ต้องแน่ใจว่า import ถูกทั้งหมด
2. **Existing data** — Database ที่มี string values จะต้อง migrate ให้เป็น int
3. **JSON serialization** — Postman/API client อาจต้องเปลี่ยนวิธี test Status values
4. **DTOs** — ถ้าต้อง return status เป็น string ใน JSON ต้องใช้ `[JsonConverter]`

---

## 💡 Tips

- Enum ใน EF Core by default เก็บเป็น `int` ซึ่ง efficient
- ถ้าต้องการเก็บเป็น `string` ใน database: ใช้ `HasConversion()` ใน OrderConfiguration
- Test ด้วย Postman หลังจาก migration เพื่อให้แน่ใจว่า API ยังทำงาน
