## 🟡 Important — ควรแก้ (6 รายการ)

- [x] **I1 — `OrderService` bypass Repository Layer**  
  สร้าง `IProductRepository` พร้อม method `GetActiveByIdsAsync`  
  เพิ่ม `RemoveItemsAsync` ใน `IOrderRepository`  
  ลบ `AppDbContext` dependency ออกจาก `OrderService` ให้ inject แค่ repository interfaces  
  **ไฟล์:** `Services/OrderService.cs`, `Repositories/`, `Services/Interfaces/`

- [x] **I2 — `POST /api/orders` คืนค่า `200 OK` แทน `201 Created`**  
  เปลี่ยน `return Ok(result)` → `return CreatedAtAction(null, result)`  
  **ไฟล์:** `Controllers/OrderController.cs`, บรรทัด 40

- [x] **I3 — `POST /api/users/register` คืนค่า `200 OK` แทน `201 Created`**  
  เปลี่ยน `return Ok(result)` → `return CreatedAtAction(null, result)`  
  **ไฟล์:** `Controllers/UsersController.cs`, บรรทัด 24

- [x] **I4 — Typo `meassage` ใน Login error response**  
  เปลี่ยน `new {meassage = ex.Message}` → `new { message = ex.Message }`  
  **ไฟล์:** `Controllers/UsersController.cs`, บรรทัด 42

- [x] **I5 — ใช้ `var` ในหลายไฟล์ (ละเมิด code style)**  
  - `Services/UserService.cs` — `GenerateJwtToken`: `var jwtKey`, `var claims`, `var key`, `var credentials`, `var token`  
  - `Controllers/UsersController.cs` — Login: `var result`  
  - `Data/DbSeeder.cs` — `var users`, `var products` ทั่วทั้งไฟล์

- [ ] **I6 — `OrderService` ใช้ SQL Server แทน PostgreSQL**  
  เปลี่ยน `options.UseSqlServer(...)` → `options.UseNpgsql(...)`  
  เพิ่ม NuGet package `Npgsql.EntityFrameworkCore.PostgreSQL`  
  อัปเดต connection string ใน `appsettings.json` ให้ตรงกับ PostgreSQL  
  **ไฟล์:** `Program.cs`, `appsettings.json`

---

## 🟡 Important — Tests (2 รายการ)

- [ ] **T1 — Tests ไม่ Dispose context เมื่อ test ล้มเหลว**  
  เปลี่ยนทุก `AppDbContext context = ...` + `context.Dispose()` ท้ายสุด  
  → `await using AppDbContext context = TestDbContextFactory.CreateFresh()`  
  **ไฟล์:** ไฟล์ test ทั้งหมดใน `Ecommerce.Tests/`

- [ ] **T2 — ไม่มี Tests สำหรับ `ApproveOrdersAsync`**  
  เพิ่ม test file ใน `Ecommerce.Tests/Services/`  
  - `ApproveOrdersAsync_WithConfirmedOrders_ChangesStatusToApproved`  
  - `ApproveOrdersAsync_WithPendingOrders_ThrowsInvalidOperationException`  
  - `ApproveOrdersAsync_WithUnknownOrderId_ThrowsKeyNotFoundException`

---

## สรุปสถานะ

| หมวด | รวม | เสร็จ |
|---|---|---|
| 🟡 Important (code) | 6 | 5 |
| 🟡 Important (tests) | 2 | 0 |