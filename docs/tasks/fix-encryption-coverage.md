# Plan: EncryptionService Test Coverage

**Target file:** `Ecommerce.Tests/Services/EncryptionServiceTests.cs`  
**Goal:** Line 0% → 100%, Branch 0% → 100%

---

## Analysis

`EncryptionService` มี 2 methods + 1 constructor มี branch ทั้งหมดดังนี้:

### Constructor
| Branch | Scenario |
|--------|----------|
| ✅ Happy path | key ถูกต้อง (16 / 24 / 32 bytes) |
| ❌ Missing key | `Encryption:Key` ไม่ได้ตั้งค่า → `InvalidOperationException` |
| ❌ Invalid length | key ที่ไม่ใช่ 16/24/32 bytes → `InvalidOperationException` |

### `Encrypt(string plainText)`
| Branch | Scenario |
|--------|----------|
| ✅ null/empty guard | return plainText ทันที |
| ✅ Normal path | return Base64 string ที่ encrypt แล้ว |

### `Decrypt(string encryptedText)`
| Branch | Scenario |
|--------|----------|
| ✅ null/empty guard | return encryptedText ทันที |
| ✅ Payload too short | `fullCipher.Length <= 16` → `CryptographicException` |
| ✅ Normal path | return plaintext เดิม |

---

## Test Cases ที่ต้องเขียน

### 1. Constructor — happy paths (3 tests)

```
Constructor_With16ByteKey_DoesNotThrow()
Constructor_With24ByteKey_DoesNotThrow()
Constructor_With32ByteKey_DoesNotThrow()
```
- สร้าง `IConfiguration` ด้วย `AddInMemoryCollection` เหมือนที่ `UserServiceTests` ทำ
- ใส่ key ขนาด 16 / 24 / 32 ASCII chars

### 2. Constructor — error paths (2 tests)

```
Constructor_MissingKey_ThrowsInvalidOperationException()
Constructor_InvalidKeyLength_ThrowsInvalidOperationException()
```
- Missing: ไม่ใส่ `Encryption:Key` ใน config
- Invalid: ใส่ key ที่มีความยาวไม่ถูกต้อง เช่น 10 chars

### 3. Encrypt — null/empty guard (2 tests)

```
Encrypt_NullInput_ReturnsNull()
Encrypt_EmptyString_ReturnsEmpty()
```
- ตรวจว่า return ค่าเดิมโดยไม่ throw

### 4. Encrypt — normal path (1 test)

```
Encrypt_ValidText_ReturnsBase64String()
```
- ตรวจว่า output เป็น valid Base64
- ตรวจว่า output ไม่เท่ากับ input (plaintext ไม่ถูก return กลับมา)

### 5. Decrypt — null/empty guard (2 tests)

```
Decrypt_NullInput_ReturnsNull()
Decrypt_EmptyString_ReturnsEmpty()
```

### 6. Decrypt — error path (1 test)

```
Decrypt_TooShortPayload_ThrowsCryptographicException()
```
- สร้าง Base64 จาก byte array ขนาด ≤ 16 bytes แล้วส่งเข้า `Decrypt`
- ตรวจว่า throw `CryptographicException`

### 7. Round-trip (2 tests)

```
EncryptThenDecrypt_ReturnsOriginalText()
EncryptThenDecrypt_ProduceDifferentCipherEachTime()
```
- Test แรก: `Decrypt(Encrypt(input)) == input`
- Test ที่สอง: เรียก `Encrypt` 2 ครั้งด้วย input เดียวกัน ผลต้องต่างกัน (random IV)

---

## โครงสร้างไฟล์

```csharp
// Ecommerce.Tests/Services/EncryptionServiceTests.cs
namespace Ecommerce.Tests.Services;

public class EncryptionServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────
    private static EncryptionService BuildService(string key = "1234567890123456") // 16 bytes
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Encryption:Key", key } })
            .Build();
        return new EncryptionService(config);
    }

    // ── Constructor tests ─────────────────────────────────────────────────
    // ...

    // ── Encrypt tests ─────────────────────────────────────────────────────
    // ...

    // ── Decrypt tests ─────────────────────────────────────────────────────
    // ...

    // ── Round-trip tests ──────────────────────────────────────────────────
    // ...
}
```

---

## ลำดับการทำ

- [ ] 1. สร้างไฟล์ `Ecommerce.Tests/Services/EncryptionServiceTests.cs`
- [ ] 2. เขียน helper `BuildService(string key)`
- [ ] 3. เขียน Constructor tests (5 tests)
- [ ] 4. เขียน `Encrypt` tests (3 tests)
- [ ] 5. เขียน `Decrypt` tests (3 tests)
- [ ] 6. เขียน Round-trip tests (2 tests)
- [ ] 7. รัน `dotnet test` และตรวจ coverage

---

## Expected Coverage After

| Metric | Before | After |
|--------|--------|-------|
| Line | 0% | ~100% |
| Branch | 0% | ~100% |
| Methods | 0 / 3 | 3 / 3 |
