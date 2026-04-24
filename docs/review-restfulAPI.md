# RESTful API Review

> รีวิวโดย GitHub Copilot — ตรวจสอบ ณ วันที่ 24 เมษายน 2026

---

## สรุปภาพรวม

| หัวข้อ | สถานะ |
|--------|-------|
| Resource naming (Noun-based URL) | ⚠️ มีปัญหา |
| HTTP Methods ถูกต้อง | ✅ ผ่าน (บางส่วน) |
| HTTP Status Codes ถูกต้อง | ❌ มีปัญหาหลายจุด |
| Stateless | ✅ ผ่าน (ใช้ JWT) |
| Consistent Error Response | ⚠️ มีปัญหา |
| API Versioning | ❌ ขาด |
| Location Header หลัง 201 | ❌ ขาด |

---

## ปัญหาที่พบ

---

### 1. ❌ ใช้ Verb ใน URL (Verb in URL Path)

**RESTful หลักการ:** URL ควรแทน **ทรัพยากร (Resource/Noun)** ไม่ใช่ **การกระทำ (Action/Verb)**

#### ปัญหาที่พบ:

| Endpoint ปัจจุบัน | ปัญหา | ควรเป็น |
|------------------|-------|---------|
| `POST /api/users/register` | `/register` เป็น verb | `POST /api/users` |
| `POST /api/users/login` | `/login` เป็น verb | `POST /api/auth/token` หรือ `POST /api/sessions` |
| `POST /api/orders/{orderNumber}/confirm` | `/confirm` เป็น verb | `PATCH /api/orders/{orderNumber}` พร้อม body `{ "status": "Confirmed" }` |
| `POST /api/admin/orders/approve` | `/approve` เป็น verb | `PATCH /api/admin/orders` พร้อม body ที่ระบุ orderNumbers และ status |

#### ตัวอย่างที่ถูกต้อง:

```
# สร้าง user ใหม่ (Register)
POST /api/users
Body: { email, firstName, lastName, password, confirmPassword, phoneNumber }

# Login → สร้าง session/token
POST /api/auth/token
Body: { email, password }
Response: { token, user }

# Confirm order → เปลี่ยน status ของ resource
PATCH /api/orders/{orderNumber}
Body: { "status": "Confirmed", "shippingAddress": "..." }

# Admin approve หลาย orders → bulk update
PATCH /api/admin/orders
Body: { "orderNumbers": ["ORD-001", "ORD-002"], "status": "Approved" }
```

---

### 2. ❌ HTTP Status Code ไม่ถูกต้องสำหรับการสร้าง Resource

**RESTful หลักการ:** `POST` ที่สร้าง resource ใหม่สำเร็จ ต้องตอบกลับด้วย **`201 Created`** ไม่ใช่ `200 OK`

#### ปัญหาที่พบในโค้ด:

**`OrdersController.cs`**
```csharp
// ❌ ปัจจุบัน — ใช้ 200 OK
return Ok(result);

// ✅ ที่ถูกต้อง — ใช้ 201 Created
return CreatedAtAction(nameof(GetOrder), new { orderNumber = result.OrderNumber }, result);
// หรือถ้ายังไม่มี GET endpoint:
return StatusCode(201, result);
```

**`UsersController.cs` (Register)**
```csharp
// ❌ ปัจจุบัน
return Ok(result);

// ✅ ที่ถูกต้อง
return StatusCode(201, result);
```

#### ตารางสรุป HTTP Status Codes ที่ควรใช้:

| สถานการณ์ | Status Code |
|----------|-------------|
| สร้าง resource สำเร็จ (POST) | `201 Created` |
| ดึงข้อมูลสำเร็จ (GET) | `200 OK` |
| แก้ไขสำเร็จ (PUT/PATCH) | `200 OK` หรือ `204 No Content` |
| ลบสำเร็จ (DELETE) | `204 No Content` |
| Login สำเร็จ (ได้ token) | `200 OK` ✅ (ยอมรับได้) |
| ข้อมูลไม่ถูกต้อง | `400 Bad Request` |
| ไม่มีสิทธิ์ (token ผิด/หมดอายุ) | `401 Unauthorized` |
| มีสิทธิ์แต่ไม่อนุญาต | `403 Forbidden` |
| ไม่พบ resource | `404 Not Found` |
| email ซ้ำ หรือ conflict | `409 Conflict` |

---

### 3. ❌ ขาด `Location` Header หลัง `201 Created`

**RESTful หลักการ:** เมื่อ `POST` สร้าง resource สำเร็จ response ต้องมี `Location` header บอก URL ของ resource ที่สร้างขึ้น

```
HTTP/1.1 201 Created
Location: /api/orders/ORD-20260424-AB123456
Content-Type: application/json
```

#### ตัวอย่างใน ASP.NET Core:
```csharp
// ✅ ใช้ CreatedAtAction จะ set Location header ให้อัตโนมัติ
return CreatedAtAction(
    actionName: nameof(GetOrder),       // ชื่อ action ที่ GET resource นี้ได้
    routeValues: new { orderNumber = result.OrderNumber },
    value: result
);
```

---

### 4. ⚠️ `PUT` vs `PATCH` — Update Order ใช้ผิดประเภท

**RESTful หลักการ:**
- `PUT` = **แทนที่ทั้งหมด** (Replace) — ต้องส่ง body ครบทุก field
- `PATCH` = **แก้ไขบางส่วน** (Partial Update) — ส่งเฉพาะ field ที่ต้องการแก้ไข

ตาม spec: `PUT /api/orders/{orderNumber}` แก้ไขแค่ `productNumber` และ `จำนวน` — นี่คือ **partial update** ควรใช้ `PATCH` มากกว่า `PUT`

```
# ✅ ควรใช้
PATCH /api/orders/{orderNumber}
Body: { "items": [{ "productId": 1, "quantity": 3 }] }
```

---

### 5. ⚠️ Error Response ไม่สม่ำเสมอ (Inconsistent Error Response)

**RESTful หลักการ:** Response error ควรมีรูปแบบเดียวกันทั้ง API

#### ปัญหาที่พบ:

**Typo ใน `UsersController.cs` Login endpoint:**
```csharp
// ❌ ปัจจุบัน — พิมพ์ผิด "meassage"
return BadRequest(new {meassage = ex.Message});

// ✅ ที่ถูกต้อง
return BadRequest(new {message = ex.Message});
```

**Error format ไม่สม่ำเสมอ:** บาง endpoint ส่ง `{ message: "..." }` แต่ไม่มี error code หรือ type ที่ชัดเจน

#### แนะนำให้ใช้รูปแบบ error response ที่สม่ำเสมอ:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Email already exists",
  "traceId": "00-abc123..."
}
```

ใน ASP.NET Core ใช้ `ProblemDetails` ได้เลย:
```csharp
return BadRequest(new ProblemDetails
{
    Status = 400,
    Title = "Validation Error",
    Detail = ex.Message
});
```

---

### 6. ❌ ขาด API Versioning

**RESTful Best Practice:** API ควรมี version เพื่อรองรับการเปลี่ยนแปลงในอนาคตโดยไม่กระทบ client เดิม

#### วิธีที่นิยม:

**URL Path Versioning (แนะนำสำหรับโปรเจกต์นี้):**
```
/api/v1/users
/api/v1/orders
/api/v1/auth/token
```

**Header Versioning:**
```
GET /api/users
Accept-Version: 1.0
```

#### ตัวอย่างใน ASP.NET Core:
```csharp
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase { ... }
```

---

### 7. ⚠️ การใส่ Role ใน URL Path (`/admin/`)

**RESTful หลักการ:** URL ควรแทน resource ไม่ใช่ role ของผู้ใช้ การใส่ `/admin/` ใน path เป็น pattern ที่ใช้กันทั่วไปและ **ยอมรับได้** แต่ทางเลือกที่ RESTful กว่าคือใช้ Authorization แทน

```
# ปัจจุบัน — ยอมรับได้ แต่ไม่ pure REST
GET /api/admin/orders

# ทางเลือก — pure REST กว่า
GET /api/orders          # ต้องมี [Authorize(Roles = "Admin")]
```

ในบริบทของโปรเจกต์นี้ที่ใช้ Basic Auth สำหรับ admin และ JWT สำหรับ user **การแยก path `/admin/` ออกมาเป็นที่ยอมรับได้** เพราะ authentication mechanism ต่างกัน

---

### 8. ⚠️ `BadRequest` ใช้สำหรับทุก Exception

**ปัญหา:** ใน `OrdersController` catch-all `Exception` จะ return `400 Bad Request` แม้แต่กรณีที่ควรเป็น `404 Not Found` (product ไม่พบ)

```csharp
// ❌ ปัจจุบัน — product ไม่พบ ก็ได้ 400
catch (Exception ex)
{
    return BadRequest(new {message = ex.Message});
}
```

```csharp
// ✅ แนะนำ — แยก exception type
catch (KeyNotFoundException ex)      // product ไม่พบ
{
    return NotFound(new {message = ex.Message});   // 404
}
catch (ArgumentException ex)         // input ผิด
{
    return BadRequest(new {message = ex.Message}); // 400
}
catch (Exception ex)
{
    return StatusCode(500, new {message = "Internal server error"}); // 500 (ไม่ expose detail)
}
```

---

## สรุปสิ่งที่ดีแล้ว ✅

- ใช้ **Nouns** เป็น base resource (`/users`, `/orders`) ถูกต้อง
- ใช้ **JWT (Stateless)** สำหรับ authentication ถูกต้องตาม REST
- ใช้ **`[FromBody]`** รับ request body ถูกต้อง
- ใช้ **Plural nouns** (`users`, `orders`) ถูกต้องตามมาตรฐาน
- ใช้ **DTO** แยก request/response model ออกจาก entity ถูกต้อง
- ใช้ `[ApiController]` ที่ handle model validation อัตโนมัติ ถูกต้อง
- **ไม่รับ `userId` จาก body** แต่ดึงจาก JWT claim ถูกต้องด้านความปลอดภัย
- **ไม่ให้ user กำหนด `UnitPrice`** เอง แต่ดึงจาก DB ถูกต้องด้านความปลอดภัย

---

## Priority แนะนำให้แก้ไข

| ลำดับ | ปัญหา | ความสำคัญ |
|-------|-------|----------|
| 1 | Typo `meassage` ใน Login | 🔴 ต้องแก้ทันที |
| 2 | Status Code 200 → 201 สำหรับ POST ที่สร้าง resource | 🔴 สำคัญมาก |
| 3 | Verb ใน URL (`/register`, `/login`, `/confirm`, `/approve`) | 🟡 สำคัญ (ถ้า pure REST) |
| 4 | `PUT` → `PATCH` สำหรับ partial update | 🟡 สำคัญ |
| 5 | เพิ่ม `Location` header หลัง 201 | 🟡 สำคัญ |
| 6 | Exception handling แยก 400 / 404 / 500 | 🟡 สำคัญ |
| 7 | API Versioning | 🟢 ควรทำ |
| 8 | Consistent error response format (ProblemDetails) | 🟢 ควรทำ |
