# Fix Error Handling (Controllers)

## เป้าหมาย
- ลดความซ้ำซ้อนของการจัดการ error ใน controller
- ทำให้ endpoint ที่พฤติกรรมใกล้กัน ตอบกลับด้วย status code และ message ที่สม่ำเสมอ
- ป้องกันการส่งรายละเอียดภายในระบบ (internal details) กลับไปยัง client โดยไม่จำเป็น

## ปัญหาที่พบ

### 1) Catch กว้างเกินไปแล้วคืน 400
อาการ:
- จับ Exception ทั่วไป แล้วคืน BadRequest (400)

จุดที่พบ:
- Controllers/OrdersController.cs (CreateOrder)
- Controllers/AdminController.cs (SearchOrders)

ผลกระทบ:
- แยกไม่ออกว่าเป็นความผิดฝั่ง client หรือ server
- ทำให้ monitoring และ debugging ยาก

แนวทางแก้:
- จับ exception แบบเจาะจงก่อน (เช่น InvalidOperationException, KeyNotFoundException)
- สำหรับ exception ที่ไม่คาดคิด ให้คืน 500 พร้อม message คงที่ เช่น Internal server error
- เก็บรายละเอียด exception จริงไว้ใน log แทนการส่งให้ client

---

### 2) Catch ซ้ำซ้อน แต่ผลลัพธ์เหมือนกัน
อาการ:
- มีหลาย catch block ที่สุดท้ายคืน status/message เดียวกันทั้งหมด

จุดที่พบ:
- Controllers/UsersController.cs (Register): InvalidOperationException และ Exception คืน 400 เหมือนกัน
- Controllers/UsersController.cs (Login): InvalidOperationException และ Exception คืน 400 เหมือนกัน

ผลกระทบ:
- โค้ดยาวขึ้นโดยไม่เพิ่มความหมาย
- ดูแลรักษายากและเสี่ยงแก้ไม่ครบในอนาคต

แนวทางแก้:
- รวม catch ที่ตอบเหมือนกันให้เหลือจุดเดียว
- ถ้าต้องแยกจริง ต้องมีเหตุผลเชิงพฤติกรรมชัดเจน (status code หรือ message ต้องต่างกันอย่างมีนัย)

---

### 3) Endpoint กลุ่มเดียวกัน map error ไม่เหมือนกัน
อาการ:
- Endpoint ที่ทำงานใกล้เคียงกัน map error ไม่เท่ากัน (บางจุดมี fallback 500 บางจุดไม่มี)

จุดที่พบ:
- Controllers/OrdersController.cs

ผลกระทบ:
- ผู้ใช้ API ได้พฤติกรรมไม่คงที่
- ฝั่ง frontend/client เขียน logic รับ error ยากขึ้น

แนวทางแก้:
- กำหนด mapping มาตรฐานร่วมกันทั้ง OrdersController เช่น
  - KeyNotFoundException -> 404
  - SecurityException -> 403
  - UnauthorizedAccessException -> 401
  - InvalidOperationException/ArgumentException -> 400
  - Exception (fallback) -> 500
- ให้ทุก action ใช้โครงเดียวกัน

---

### 4) Message ไม่สม่ำเสมอในกรณี unauthorized
อาการ:
- บางจุดใช้ข้อความคงที่ เช่น Invalid token
- บางจุดส่ง ex.Message ตรงๆ

ผลกระทบ:
- client handle ข้อความยาก
- มีความเสี่ยงเปิดเผยรายละเอียดระบบ

แนวทางแก้:
- แยก message สำหรับ client เป็นข้อความมาตรฐานเดียว
- ใช้ ex.Message เฉพาะกรณีที่ตั้งใจและปลอดภัย
- ตัวอย่างชุดข้อความแนะนำ:
  - 401: Unauthorized
  - 403: Forbidden
  - 404: Resource not found
  - 400: Invalid request
  - 500: Internal server error

## แนวทาง implementation (แนะนำ)

### ระยะที่ 1: ปรับเฉพาะ Controller (ทำได้เร็ว)
1. แก้จุดที่ catch Exception แล้วคืน 400 ให้เป็น 500
2. ลบ/รวม catch block ที่ซ้ำซ้อนใน UsersController
3. ทำให้ทุก action ใน OrdersController มี fallback 500 แบบเดียวกัน
4. ใช้รูปแบบ response เดียวกันเสมอ: { "message": "..." }

### ระยะที่ 2: รวมศูนย์ Error Handling
1. สร้าง Global Exception Middleware หรือ Exception Filter
2. ย้าย mapping exception -> status code ไปไว้จุดกลาง
3. ลด try/catch ใน controller ให้เหลือเฉพาะกรณีพิเศษ

### ระยะที่ 3: Logging และการทดสอบ
1. เพิ่ม structured logging ตอนเกิด exception
2. เพิ่ม unit/integration tests สำหรับ status code แต่ละกรณี
3. ทดสอบ regression กับ endpoint สำคัญ: register, login, create/update/confirm order, admin approve/search

## ตัวอย่าง policy กลาง (ฉบับย่อ)
- ตอบกลับด้วยโครงเดียวกันทุก error: { "message": "..." }
- ไม่ส่ง stack trace หรือรายละเอียดภายในให้ client
- ถ้าเป็น unknown error ให้ตอบ 500 และ log รายละเอียดในระบบ
- ถ้าเป็น domain/business rule violation ให้ตอบ 400 พร้อมข้อความที่ผู้ใช้เข้าใจได้

## Checklist ก่อนปิดงาน
- [ ] ไม่มี catch (Exception ex) ที่คืน 400 โดยไม่มีเหตุผล
- [ ] ไม่มี catch ซ้ำซ้อนที่คืนผลเหมือนกัน
- [ ] Endpoint ใน controller เดียวกัน map error แบบเดียวกัน
- [ ] message สำหรับ client ใช้มาตรฐานเดียวกัน
- [ ] มี test ครอบคลุมกรณี 400/401/403/404/500 ที่จำเป็น
