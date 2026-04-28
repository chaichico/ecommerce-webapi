# Refactor Checklist — Ecommerce API

อ้างอิงจาก [code-review-thai.md](./code-review-thai.md)  
**สถานะ:** 🔲 ยังไม่ได้ทำ · ✅ เสร็จแล้ว

---

## 🔴 Critical — ต้องแก้ก่อน (5 รายการ)

- [x] **C1 — Hardcoded secrets ใน `appsettings.json`**  
  ล้างค่า `Jwt:Key`, `Encryption:Key`, `Encryption:IV`, `AdminAuth:Username/Password` ให้เป็นค่าว่าง  
  ลบ block `AdminCredentials` ที่เป็น dead code  
  **ไฟล์:** `appsettings.json`

- [x] **C2 — AES Static IV ใน `EncryptionService`**  
  เปลี่ยนให้ `GenerateIV()` ทุกครั้งที่ Encrypt, prepend IV 16 bytes ต้น ciphertext, อ่าน IV กลับตอน Decrypt  
  ลบ field `_iv` และ `Encryption:IV` config key ออกทั้งหมด  
  **ไฟล์:** `Services/EncryptionService.cs`

- [x] **C3 — Hardcoded fallback secrets ใน `EncryptionService`**  
  เปลี่ยน `?? "YourSecretKey..."` เป็น `?? throw new InvalidOperationException("Encryption:Key is not configured")`  
  **ไฟล์:** `Services/EncryptionService.cs`, บรรทัด 14–15

- [x] **C4 — Timing Attack ใน `AdminController.IsAuthorized()`**  
  เปลี่ยนจากการเปรียบเทียบ `==` เป็น `CryptographicOperations.FixedTimeEquals(...)`  
  **ไฟล์:** `Controllers/AdminController.cs`, บรรทัด 36–37

- [x] **C5 — Seed data มี fake password hash**  
  ใช้ `IPasswordHasher.HashPassword(...)` จริงใน `DbSeeder.cs` แทน string เช่น `"hashed_password_1"`  
  **ไฟล์:** `Data/DbSeeder.cs`, บรรทัด 21–23

---





## สรุปสถานะ

| หมวด | รวม | เสร็จ |
|---|---|---|
| 🔴 Critical | 5 | 5 |

