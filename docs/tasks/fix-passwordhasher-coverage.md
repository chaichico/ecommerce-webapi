# Plan: Fix PasswordHasher Coverage

**Target file:** `Ecommerce.Tests/Services/PasswordHasherTests.cs` (สร้างใหม่)  
**Goal:** Line coverage 0% → 100%

---

## สาเหตุที่ coverage เป็น 0%

`PasswordHasher` ยังไม่มี test file เลย — `UserServiceTests` mock `IPasswordHasher` ออกไปทั้งหมด
ทำให้ implementation จริงใน `PasswordHasher.cs` ไม่เคยถูกเรียกเลย

---

## สิ่งที่ต้อง Test

### `HashPassword(string password)`

| # | Test Case | Expected |
|---|-----------|----------|
| 1 | password ธรรมดา | return string ที่ไม่ null/empty |
| 2 | ตรวจ format `{salt}.{hash}` | มี `.` คั่น และ split ได้ 2 ส่วน |
| 3 | เรียก 2 ครั้งด้วย password เดิม | ได้ผลลัพธ์ต่างกัน (random salt) |

### `VerifyPassword(string password, string passwordHash)`

| # | Test Case | Expected |
|---|-----------|----------|
| 4 | password ถูกต้อง (hash มาจาก HashPassword) | return `true` |
| 5 | password ผิด | return `false` |
| 6 | password ว่าง hash ด้วย password ว่าง | return `true` |

---

## ขั้นตอนการทำ

1. สร้างไฟล์ `Ecommerce.Tests/Services/PasswordHasherTests.cs`
2. สร้าง instance จริงของ `PasswordHasher` (ไม่ต้อง mock — เป็น pure computation)
3. เขียน test แต่ละ case ตามตารางด้านบน
4. รัน test และตรวจ coverage

---

## ตัวอย่างโครงสร้าง Test

```csharp
using Services;

namespace Ecommerce.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    // HashPassword — returns non-null result in "salt.hash" format
    [Fact]
    public void HashPassword_ValidPassword_ReturnsFormattedHash() { ... }

    // HashPassword — two calls produce different hashes (random salt)
    [Fact]
    public void HashPassword_CalledTwice_ReturnsDifferentHashes() { ... }

    // VerifyPassword — correct password returns true
    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue() { ... }

    // VerifyPassword — wrong password returns false
    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse() { ... }

    // VerifyPassword — empty password hashed and verified
    [Fact]
    public void VerifyPassword_EmptyPassword_ReturnsTrue() { ... }
}
```

---

## Expected Coverage หลังทำเสร็จ

| Metric | Before | After |
|--------|--------|-------|
| Line   | 0%     | 100%  |
| Branch | 100%   | 100%  |

> Branch ยังคง 100% เพราะ `PasswordHasher` ไม่มี branch logic อยู่แล้ว
