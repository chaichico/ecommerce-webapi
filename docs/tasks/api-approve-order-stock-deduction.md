# Stock Deduction on Admin Approve Order

## Overview

เพิ่ม logic หัก `Stock` ของ `Product` เมื่อ Admin approve order โดยจะหักเฉพาะ orders ที่มีสถานะ **Pending** เท่านั้น

## Flow

```
Admin POST /api/admin/orders/approve
    → ApproveOrdersAsync()
        → ตรวจสอบว่าพบทุก OrderId
        → โหลด Products ที่เกี่ยวข้อง
        → ตรวจสอบ Stock ว่าเพียงพอ (ทุก order ก่อน approve)
        → หัก Stock และเปลี่ยน Status → "Confirmed"
        → SaveChangesAsync() (บันทึก orders + products พร้อมกัน)
```

## ไฟล์ที่แก้ไข

| ไฟล์ | สิ่งที่เปลี่ยน |
|------|----------------|
| `Services/OrderService.cs` | เพิ่ม logic ตรวจ stock และหัก stock ใน `ApproveOrdersAsync` |
| `Controllers/AdminController.cs` | เพิ่ม `catch (InvalidOperationException)` สำหรับกรณี stock ไม่พอ |

## Business Rules

- หัก stock **เฉพาะตอน Admin Approve** (ไม่หักตอนสร้าง order)
- Orders ที่สถานะไม่ใช่ `Pending` จะถูกข้ามไป ไม่มีการหัก stock ซ้ำ
- ตรวจสอบ stock ของ **ทุก order ก่อน** ก่อนที่จะหักจริง — ป้องกัน partial update (หัก order แรกแล้ว order ที่สองไม่พอ)
- ถ้า stock ไม่พอ จะ return `400 Bad Request` พร้อม message ระบุชื่อสินค้า, จำนวนที่ต้องการ, และจำนวนที่มีอยู่

## Error Response

```json
{
  "message": "Insufficient stock for 'Product Name': required 5, available 2"
}
```
