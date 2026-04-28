## 🟢 Suggestions — ทำเพิ่มถ้ามีเวลา (5 รายการ)

- [ ] **S1 — Order status เป็น magic strings**  
  สร้าง `public static class OrderStatus` พร้อม constants `Pending`, `Confirmed`, `Approved`  
  **ไฟล์:** สร้างไฟล์ใหม่ใน `Models/` หรือ `Models/Enums/`

- [ ] **S2 — Duplicate user lookup pattern ใน `OrderService`**  
  แยก private helper `GetUserByEmailOrThrowAsync(string email)` ใน `OrderService`

- [ ] **S3 — Typo parameter `dtor` ใน `IOrderService`**  
  เปลี่ยน `UpdateOrderDto dtor` → `UpdateOrderDto dto`  
  **ไฟล์:** `Services/Interfaces/IOrderService.cs`, บรรทัด 7

- [ ] **S4 — ชื่อไฟล์ไม่ตรงกับชื่อ class**  
  เปลี่ยนชื่อไฟล์ `OrderController.cs` → `OrdersController.cs`

- [ ] **S5 — `ApproveOrdersAsync` บันทึก orders ที่ไม่มีการเปลี่ยนแปลง**  
  เปลี่ยน `UpdateRangeAsync(orders)` → `UpdateRangeAsync(confirmedOrders)`  
  **ไฟล์:** `Services/OrderService.cs`

- [ ] **S6 — Comment เหลือทิ้งท้าย `OrderService.cs`**  
  ลบ development notes ที่อยู่นอก class หลัง `}` สุดท้าย  
  **ไฟล์:** `Services/OrderService.cs`, 5 บรรทัดสุดท้าย

- [ ] **S7 — หมายเลข step comment ใน `ApproveOrdersAsync` ผิดลำดับ**  
  จัดเรียง step comments ให้ถูกต้อง (ขาด step 7–8)  
  **ไฟล์:** `Services/OrderService.cs`

---

## สรุปสถานะ

| หมวด | รวม | เสร็จ |
|---|---|---|
| 🟢 Suggestions | 7 | 0 |
