# Last Refactor Checklist

Based on: [last-review.md](last-review.md)
Date: 2026-04-29

## 🔴 CRITICAL

### Security/REST: Fix `CreatedAtAction(null, ...)` in create endpoints
- [x] Controllers/OrdersController.cs:37 — Add GET endpoint and fix CreatedAtAction
- [x] Controllers/UsersController.cs:26 — Add GET endpoint and fix CreatedAtAction
- **Action:** Return `CreatedAtAction(nameof(GetById), new { id = ... }, responseDto)`

### Security: Enforce security key validation at startup
- [x] appsettings.json:13 — Configure `AdminAuth:Username` via environment variable
- [x] appsettings.json:14 — Configure `AdminAuth:Password` via environment variable
- [x] appsettings.json:17 — Configure `Jwt:Key` via environment variable
- [x] appsettings.json:23 — Configure `Encryption:Key` via environment variable
- **Action:** Add fail-fast validation in Program.cs to ensure no empty/default secrets at startup

---

## 🟡 IMPORTANT

### Error Handling: Fix malformed Basic auth header handling
- [x] Controllers/AdminController.cs:30 — Wrap `Convert.FromBase64String()` in try/catch
- **Action:** Return 401 Unauthorized on `FormatException` instead of 500

### Security Hardening: Validate AES key length at initialization
- [x] Services/EncryptionService.cs:15 — Add explicit key length validation (16/24/32 bytes)
- **Action:** Throw `InvalidOperationException` with clear message if invalid

### Error Handling: Replace generic `Exception` with specific types
- [x] Services/OrderService.cs:37 — Use `InvalidOperationException` for invalid state
- [x] Services/OrderService.cs:121 — Use `InvalidOperationException` for invalid state
- [x] Services/UserService.cs:32 — Use `InvalidOperationException` for invalid state
- [x] Services/UserService.cs:101 — Use `InvalidOperationException` for invalid state
- **Action:** Update exception types and audit controller mappings to status codes

### REST Coverage: Add GET endpoint for single order resource
- [x] Controllers/OrdersController.cs — Add `GET /api/orders/{id}` with ownership check
- **Action:** Return `OrderResponseDto` with proper DTO mapping

### Data Protection: Encrypt phone numbers in seeded data
- [x] Data/DbSeeder.cs:26 — Encrypt phone before persistence
- [x] Data/DbSeeder.cs:34 — Encrypt phone before persistence
- [x] Data/DbSeeder.cs:42 — Encrypt phone before persistence
- **Action:** Inject `IEncryptionService` and encrypt seeded phone values

---

## 🟢 SUGGESTIONS

### Validation: Increase shipping address minimum length
- [x] Models/Dtos/ConfirmOrderDto.cs:8 — Raise `MinLength` from 1 to 10
- **Action:** Consider business alignment on address format validation rules

---

## Summary

- **Critical fixes:** 2 (CreatedAtAction + security keys)
- **Important fixes:** 5 (error handling, validation, REST coverage, data protection)
- **Suggestions:** 1 (validation strengthening)

**Total actionable items:** 9
