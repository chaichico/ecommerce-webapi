# กำหนดค่า InMemory Database สำหรับ Testing

## สถานะปัจจุบัน

สิ่งที่มีอยู่แล้ว:
- ✅ Package `Microsoft.EntityFrameworkCore.InMemory` ติดตั้งใน `Ecommerce.Tests.csproj`
- ✅ `Ecommerce.Tests/Helpers/TestDbContextFactory.cs` — helper สร้าง InMemory `AppDbContext`
- ✅ `appsettings.Test.json` — มี `RunMode: "test"` และ `ConnectionStrings.DefaultConnection: "InMemory"`
- ✅ `UnitTest1.cs` — test เบื้องต้นว่า InMemory context สร้างได้

สิ่งที่ยังขาด:
- ❌ `AppDbContext.OnModelCreating` ว่างเปล่า — ไม่มี Entity Configurations ถูก apply
- ❌ ไม่มี Test Seed Data helper สำหรับใส่ข้อมูลทดสอบ
- ❌ ไม่มี Unit Tests จริงสำหรับ Repository / Service layer
- ❌ `Program.cs` ยังคง `UseSqlServer` เสมอ — ไม่ switch เป็น InMemory เมื่อ `RunMode=test`

---

## Steps

### Step 1 — สร้าง Entity Type Configurations

สร้างไฟล์ Configuration สำหรับแต่ละ Model ใน `Data/Configurations/`  
ใช้ `IEntityTypeConfiguration<T>` ตาม project convention

**ไฟล์ที่ต้องสร้าง:**

- `Data/Configurations/UserConfiguration.cs`
  - Email → unique index, required, max length 255
  - FirstName, LastName → required, max length 100
  - PhoneNumber → nullable, max length 500 (encrypted)
  - PasswordHash → required

- `Data/Configurations/ProductConfiguration.cs`
  - Name → required, max length 200
  - Price → required, precision(18,2)
  - Status → required, default "active"

- `Data/Configurations/OrderConfiguration.cs`
  - OrderNumber → unique index, required, max length 50
  - Status → required, default "รอยืนยัน"
  - UserId → FK → User
  - ShippingAddress → nullable

- `Data/Configurations/OrderItemConfiguration.cs`
  - OrderId → FK → Order
  - ProductId → FK → Product
  - Quantity → required
  - UnitPrice → required, precision(18,2)

### Step 2 — แก้ไข AppDbContext.OnModelCreating

เพิ่มการ apply configurations ทั้งหมดใน `Data/AppDbContext.cs`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

> `ApplyConfigurationsFromAssembly` จะ scan และ apply ทุก `IEntityTypeConfiguration<T>` ใน assembly อัตโนมัติ

### Step 3 — สร้าง Test Seed Data Helper

สร้างไฟล์ `Ecommerce.Tests/Helpers/TestDataSeeder.cs`

ควรมี static methods สำหรับใส่ข้อมูลทดสอบ เช่น:

```csharp
public static class TestDataSeeder
{
    public static User CreateUser(AppDbContext context, string email = "test@example.com") { ... }
    public static Product CreateProduct(AppDbContext context, string name = "Test Product") { ... }
    public static Order CreateOrder(AppDbContext context, int userId) { ... }
}
```

ใช้คู่กับ `TestDbContextFactory.CreateFresh()` เพื่อให้แต่ละ test มี database แยกกัน (ไม่มี state leak)

### Step 4 — เขียน Unit Tests

สร้างไฟล์ test แยกตาม layer ใน `Ecommerce.Tests/`:

**Repository Tests** — `Repositories/UserRepositoryTests.cs`, `OrderRepositoryTests.cs`
- ทดสอบ CRUD operations โดยตรงกับ InMemory DB
- ใช้ `TestDbContextFactory.CreateFresh()` ใน constructor / setup ของแต่ละ test class

**Service Tests** — `Services/UserServiceTests.cs`, `OrderServiceTests.cs`
- ทดสอบ business logic ใน Service layer
- Mock dependencies (เช่น `IPasswordHasher`, `IEncryptionService`) ด้วย Fake class หรือ library เช่น Moq
- ใช้ InMemory `AppDbContext` แทน PostgreSQL จริง

ตัวอย่างโครงสร้าง test:
```csharp
public class UserRepositoryTests
{
    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ReturnsUser()
    {
        AppDbContext context = TestDbContextFactory.CreateFresh();
        // Arrange: seed data ...
        // Act: call repository method ...
        // Assert: verify result ...
        context.Dispose();
    }
}
```

### Step 5 — (Optional) Integration Test ด้วย WebApplicationFactory

ถ้าต้องการทดสอบ HTTP endpoint แบบ end-to-end ใน test:

1. ติดตั้ง package `Microsoft.AspNetCore.Mvc.Testing` ใน `Ecommerce.Tests.csproj`

2. แก้ไข `Program.cs` ให้ switch ใช้ InMemory DB เมื่อ `RunMode=test`:

```csharp
string? runMode = builder.Configuration["RunMode"];
if (runMode == "test")
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("TestDb"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}
```

3. สร้าง `Ecommerce.Tests/IntegrationTests/CustomWebApplicationFactory.cs` ที่ใช้ `appsettings.Test.json`

4. เขียน Integration Test ที่ส่ง HTTP request จริงผ่าน `HttpClient` ที่ได้จาก factory

---

## ลำดับการทำ

1. [ ] Step 1 — สร้าง Entity Configurations ใน `Data/Configurations/`
2. [ ] Step 2 — แก้ไข `AppDbContext.OnModelCreating`
3. [ ] Step 3 — สร้าง `TestDataSeeder.cs`
4. [ ] Step 4 — เขียน Unit Tests (Repository และ Service)
5. [ ] Step 5 — (Optional) Integration Tests ด้วย WebApplicationFactory
