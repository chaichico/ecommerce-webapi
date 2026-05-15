# Code Coverage Report

**Date:** 2026-05-15  
**Tool:** coverlet.collector + XPlat Code Coverage  
**Report source:** `Ecommerce.Tests/TestResults/.../coverage.cobertura.xml`

---

## Overall Project Coverage

| Metric | Covered | Total | Rate |
|--------|---------|-------|------|
| Lines | 311 | 2,377 | **13.1%** |
| Branches | 54 | 170 | **31.8%** |

> ⚠️ อัตรารวมต่ำเพราะ coverage วัดทั้ง project (Controllers, Repositories, Middleware, Program.cs ฯลฯ) แต่ test มีแค่ Services

---

## Service-Level Coverage

| Service | Line Coverage | Branch Coverage | Methods Covered |
|---------|:------------:|:---------------:|:---------------:|
| `OrderService` | ✅ 100% | ✅ 100% | 25 / 25 |
| `UserService` | ✅ 100% | ⚠️ 50% | 4 / 4 |
| `PasswordHasher` | ❌ 0% | ✅ 100% | 0 / 2 |
| `EncryptionService` | ❌ 0% | ❌ 0% | 0 / 3 |

---

## Detail by Service

### OrderService ✅
- **Line:** 100% | **Branch:** 100%
- Methods ครบทุกตัว: `CreateOrderAsync`, `UpdateOrderAsync`, `ConfirmOrderAsync`, `ApproveOrdersAsync`, `GetOrderByIdAsync`, `SearchOrdersAsync`, `GetUserByEmailOrThrowAsync`
- มี 1 method ที่ branch coverage 87.5% (`UpdateOrderAsync` — มี branch ที่ยังไม่ครบ 1 เส้นทาง)

---

### UserService ⚠️
- **Line:** 100% | **Branch:** 50%
- Methods ครบทุกตัว: `RegisterAsync`, `LoginAsync`
- `GenerateJwtToken` — branch coverage **50%** คือยังไม่มี test ที่ครอบคลุม edge case ทั้งหมด (เช่น token expiry config ที่ missing หรือ claim ที่ optional)

---

### PasswordHasher ❌
- **Line:** 0% | **Branch:** 100%
- ยังไม่มี test เลย — `HashPassword` และ `VerifyPassword` ยังไม่ถูก test โดยตรง
- Branch 100% มาจากที่ไม่มี branch logic ใน class (เป็น pure computation)

---

### EncryptionService ❌
- **Line:** 0% | **Branch:** 0%
- ยังไม่มี test เลย ทุก method ยังไม่ถูกแตะ

---

## สิ่งที่ควรเพิ่ม Test

| Priority | Service | สิ่งที่ขาด |
|----------|---------|------------|
| 🔴 High | `EncryptionService` | Test encrypt/decrypt ทั้งหมด |
| 🔴 High | `PasswordHasher` | Test `HashPassword` และ `VerifyPassword` |
| 🟡 Medium | `UserService` | เพิ่ม test สำหรับ `GenerateJwtToken` edge cases |
| 🟢 Low | `OrderService` | เพิ่ม test สำหรับ `UpdateOrderAsync` branch ที่เหลือ |
