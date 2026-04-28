# Code Review Report

Review language: English
Date: 2026-04-28
Scope: Current workspace snapshot (controllers, services, repositories, data seeding, configuration, and tests)

## Findings

**🔴 CRITICAL - Security/REST: `CreatedAtAction(null, ...)` used in create endpoints**

`CreatedAtAction` is called with a null action name in both create endpoints.

- Controllers/OrdersController.cs:37
- Controllers/UsersController.cs:26

**Why this matters:**
`201 Created` responses should include a valid `Location` header pointing to the created resource. Passing `null` weakens API contract clarity and breaks REST discoverability for clients.

**Suggested fix:**
1. Add concrete GET endpoints for created resources.
2. Return `CreatedAtAction(nameof(GetById), new { id = ... }, responseDto)` (or equivalent action name/route values).

**Reference:** RFC 9110 (HTTP Semantics) - 201 Created; ASP.NET Core `CreatedAtAction` usage guidance

---

**🔴 CRITICAL - Security: Sensitive security keys are blank/default in app config**

Security-critical config values are present as empty strings in application configuration.

- appsettings.json:13
- appsettings.json:14
- appsettings.json:17
- appsettings.json:23

**Why this matters:**
Empty/default secrets can cause insecure startup behavior, accidental deployments with broken auth/encryption, or runtime failures. Project rules also require sensitive credentials to come from environment variables.

**Suggested fix:**
1. Do not store admin credentials or real secrets in `appsettings.json`.
2. Enforce fail-fast validation at startup for `AdminAuth`, `Jwt:Key`, and `Encryption:Key`.
3. Use environment variables / secret store per environment.

**Reference:** OWASP Secrets Management; project guideline "Configuration" and "Authentication"

---

**🟡 IMPORTANT - Security/Correctness: Basic auth parser can throw on malformed header**

Admin auth decoding uses `Convert.FromBase64String(...)` without guarding malformed input.

- Controllers/AdminController.cs:30

**Why this matters:**
A malformed `Authorization` header can throw `FormatException`, causing a 500 instead of a clean 401 response path.

**Suggested fix:**
Wrap decode in try/catch and return unauthorized on parse failure.

```csharp
try
{
    string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
    ...
}
catch (FormatException)
{
    return false;
}
```

**Reference:** Defensive parsing for auth headers (ASP.NET Core security best practices)

---

**🟡 IMPORTANT - Correctness/Security Hardening: AES key length is not validated before use**

Encryption key bytes are loaded directly from config and assigned to AES key without explicit validation.

- Services/EncryptionService.cs:15

**Why this matters:**
Invalid key lengths (not 16/24/32 bytes) fail at runtime with cryptographic exceptions that are harder to diagnose and may surface during request execution instead of startup.

**Suggested fix:**
Validate key size during service construction and throw an explicit configuration error.

```csharp
_key = Encoding.UTF8.GetBytes(keyString);
if (_key.Length != 16 && _key.Length != 24 && _key.Length != 32)
{
    throw new InvalidOperationException("Encryption:Key must be 16, 24, or 32 bytes");
}
```

**Reference:** .NET AES key size requirements

---

**🟡 IMPORTANT - Error Handling: Generic `Exception` used for domain validation failures**

Business validation failures throw `Exception` instead of a specific exception type.

- Services/OrderService.cs:37
- Services/OrderService.cs:121
- Services/UserService.cs:32
- Services/UserService.cs:101

**Why this matters:**
Generic exceptions reduce error semantics, make tests less precise, and weaken controller-to-status-code mapping.

**Suggested fix:**
Use targeted exceptions, e.g.:
- `InvalidOperationException` for invalid state transitions
- `ArgumentException` for invalid request arguments
- `UnauthorizedAccessException` only for authentication/authorization failures

**Reference:** .NET exception design guidelines

---

**🟡 IMPORTANT - Architecture/REST Coverage: No GET endpoint in Orders controller**

Orders controller exposes only POST/PUT/POST(confirm) operations.

- Controllers/OrdersController.cs:22
- Controllers/OrdersController.cs:50
- Controllers/OrdersController.cs:87

**Why this matters:**
Without a `GET /api/orders/{id}` endpoint, clients cannot retrieve a single order resource via standard REST flows, and create responses cannot link to a canonical read endpoint.

**Suggested fix:**
Add `GET /api/orders/{id}` with ownership checks and DTO response.

**Reference:** Project REST conventions; standard resource-oriented API design

---

**🟡 IMPORTANT - Data Protection: Seeder stores phone numbers in plain text**

Seeded users include raw phone numbers.

- Data/DbSeeder.cs:26
- Data/DbSeeder.cs:34
- Data/DbSeeder.cs:42

**Why this matters:**
Project rules state sensitive fields (phone number) should use `IEncryptionService`. Plain text seed values can normalize insecure data-at-rest patterns.

**Suggested fix:**
Encrypt seeded phone values before persistence, or seed without phone values in shared/dev data.

**Reference:** Project guideline "Authentication" (sensitive fields encryption)

---

**🟢 SUGGESTION - Validation: Shipping address minimum length is too permissive**

Current minimum length allows a one-character shipping address.

- Models/Dtos/ConfirmOrderDto.cs:8

**Why this matters:**
Very weak validation increases invalid order confirmations and avoidable downstream failures.

**Suggested fix:**
Raise minimum length (for example, 10) and consider additional format rules aligned with business requirements.

**Reference:** Input validation best practices

---

## Positive Notes

1. `OrderItem.SubTotal` exists and is correctly implemented as a computed property.
- Models/OrderItem.cs:31

2. Service/repository test coverage is broader than a single smoke test (multiple focused tests across order/user services and repositories).
- Ecommerce.Tests/Services/OrderServiceTests.cs:11
- Ecommerce.Tests/Services/ApproveOrdersAsyncTests.cs:11
- Ecommerce.Tests/Services/UserServiceTests.cs:12
- Ecommerce.Tests/Repositories/OrderRepositoryTests.cs:9
- Ecommerce.Tests/Repositories/UserRepositoryTests.cs:8

3. Password hashing implementation uses PBKDF2 with random salt and a strong iteration count.
- Services/PasswordHasher.cs:11

## Test Execution Status

Automated test execution could not complete because build output was locked by a running process (`ecommerce.exe`), causing MSB3026/MSB3027/MSB3021 copy failures during `dotnet test`.

- Impact: This review includes static analysis plus source-level test inspection, but not a successful end-to-end test run in this session.

## Open Questions / Assumptions

1. Is plain text phone seeding accepted for local-only demo data, or must all persisted phone values always be encrypted in every environment?
2. Should `OrderNumber` remain the public primary identifier for API responses, or should numeric `Id` also be returned consistently?
3. Should admin auth stay as custom Basic header parsing, or migrate to an ASP.NET Core auth handler/policy for consistency with JWT pipeline?

## Summary

- Critical: 2
- Important: 5
- Suggestion: 1

Primary merge blockers are REST contract correctness around create responses and secret/config hardening.
