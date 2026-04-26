# Add `Approved` Status for Admin

## Order Status Flow (ใหม่)

```
Pending → (User Confirm) → Confirmed → (Admin Approve) → Approved
```

| Status | ความหมาย | เปลี่ยนโดย |
|---|---|---|
| `Pending` | รอ user confirm | Create Order |
| `Confirmed` | User ยืนยันแล้ว รอ admin | User Confirm Order |
| `Approved` | Admin ตรวจสต็อกแล้ว พร้อมส่ง | Admin Approve Order |

---

## Changes Required

### `Services/OrderService.cs` — `ApproveOrdersAsync`

1. เปลี่ยน filter orders จาก `Status == "Pending"` → `Status == "Confirmed"`
2. เปลี่ยน `order.Status = "Confirmed"` → `order.Status = "Approved"`

### ไม่ต้องเปลี่ยน

- `ConfirmOrderAsync` — ยังคง check `Pending` และ set `Confirmed` เหมือนเดิม
- `UpdateOrderAsync` — แก้ order ได้แค่ตอน `Pending` เหมือนเดิม
- `Models/Order.cs` — Status เป็น `string` ไม่ต้องเปลี่ยน
