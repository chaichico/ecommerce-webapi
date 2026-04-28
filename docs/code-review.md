# Code Review — Ecommerce API

**Date:** 2026-04-28  
**Reviewer:** GitHub Copilot  
**Tech Stack:** .NET 10 Web API · Entity Framework Core · SQL Server · JWT Bearer Authentication  
**Scope:** Full codebase review — security, architecture, code quality, testing, performance

---

## Summary

The codebase follows a clear layered architecture and demonstrates solid REST API design. JWT authentication, password hashing, and DTO patterns are properly applied. However, there are **critical security issues** (hardcoded secrets, static IV in AES), an **architecture violation** (service bypasses the repository layer), and several **important bugs** (typo in error response, wrong HTTP status code on create, var usage) that must be addressed before production deployment.

---

## 🔴 CRITICAL Issues

---

### 🔴 CRITICAL — Security: Hardcoded Secrets in `appsettings.json`

**File:** `appsettings.json`

```json
"Jwt": {
  "Key": "YourSuperSecretKeyForJWT1234567890"
},
"Encryption": {
  "Key": "12345678901234567890123456789012",
  "IV":  "1234567890123456"
},
"AdminAuth": {
  "Username": "admin",
  "Password": "admin1234"
}
```

Also present are duplicate admin credential blocks (`AdminCredentials` and `AdminAuth`) — one appears to be dead code.

**Why this matters:**  
Secrets committed to source control can be extracted from git history even after deletion. The JWT key is the signing secret for all user tokens. The encryption key protects PII (phone numbers). Any developer or CI/CD system with repository access can impersonate users or decrypt stored PII.

**Suggested fix:**  
Move all secrets to environment variables or a secrets manager. Use placeholder values in `appsettings.json` and document the required variables in README:

```json
"Jwt": { "Key": "" },
"Encryption": { "Key": "", "IV": "" },
"AdminAuth": { "Username": "", "Password": "" }
```

Set values via environment variables or `dotnet user-secrets` locally:
```bash
dotnet user-secrets set "Jwt:Key" "<random-256-bit-value>"
```

Remove the dead `AdminCredentials` block entirely.

**Reference:** [OWASP: Sensitive Data Exposure](https://owasp.org/Top10/A02_2021-Cryptographic_Failures/)

---

### 🔴 CRITICAL — Security: Static AES IV in `EncryptionService.cs`

**File:** `Services/EncryptionService.cs`, lines 8–20

```csharp
string ivString = configuration["Encryption:IV"] ?? "YourIV1234567890"; // 16 chars
_iv = Encoding.UTF8.GetBytes(ivString);
// ...
aes.IV = _iv; // same IV used for every encryption
```

**Why this matters:**  
AES-CBC with a fixed IV reveals patterns: two users with the same phone number will produce identical ciphertexts. This completely defeats the purpose of encryption for PII data. Additionally, the `?? "YourIV1234567890"` fallback hardcodes a secret in source code.

**Suggested fix:**  
Generate a fresh random IV per encryption, prepend it to the ciphertext, and read it back during decryption:

```csharp
public string Encrypt(string plainText)
{
    using Aes aes = Aes.Create();
    aes.Key = _key;
    aes.GenerateIV(); // fresh random IV every time

    ICryptoTransform encryptor = aes.CreateEncryptor();
    using MemoryStream ms = new MemoryStream();
    ms.Write(aes.IV, 0, aes.IV.Length); // prepend IV (16 bytes)
    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
    using (StreamWriter sw = new StreamWriter(cs))
        sw.Write(plainText);

    return Convert.ToBase64String(ms.ToArray());
}

public string Decrypt(string encryptedText)
{
    byte[] data = Convert.FromBase64String(encryptedText);
    byte[] iv = data[..16];
    byte[] cipherBytes = data[16..];

    using Aes aes = Aes.Create();
    aes.Key = _key;
    aes.IV = iv;

    ICryptoTransform decryptor = aes.CreateDecryptor();
    using MemoryStream ms = new MemoryStream(cipherBytes);
    using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
    using StreamReader sr = new StreamReader(cs);
    return sr.ReadToEnd();
}
```

Remove the `IV` key from configuration entirely — it is no longer needed.

**Reference:** [NIST SP 800-38A — AES-CBC Mode](https://csrc.nist.gov/publications/detail/sp/800-38a/final)

---

### 🔴 CRITICAL — Security: Hardcoded Fallback Secrets in `EncryptionService.cs`

**File:** `Services/EncryptionService.cs`, lines 14–15

```csharp
string keyString = configuration["Encryption:Key"] ?? "YourSecretKey1234567890123456";
string ivString  = configuration["Encryption:IV"]  ?? "YourIV1234567890";
```

**Why this matters:**  
If the configuration key is absent (e.g., misconfigured environment), the code silently falls back to known hardcoded secrets. This means a misconfiguration produces a false sense of security — data is "encrypted" but trivially decryptable by anyone who reads the source code.

**Suggested fix:**  
Throw an exception if the key is not configured, following the same pattern already used in `UserService.GenerateJwtToken`:

```csharp
string keyString = configuration["Encryption:Key"]
    ?? throw new InvalidOperationException("Encryption:Key is not configured");
```

---

### 🔴 CRITICAL — Security: Timing Attack Risk in `AdminController.IsAuthorized()`

**File:** `Controllers/AdminController.cs`, lines 36–37

```csharp
return parts[0] == username && parts[1] == password;
```

**Why this matters:**  
Ordinary string equality (`==`) returns early on the first byte mismatch, which makes the response time dependent on how many leading characters match. An attacker can measure response times to brute-force the admin credentials one character at a time.

**Suggested fix:**  
Use constant-time comparison:

```csharp
using System.Security.Cryptography;

bool usernameMatch = CryptographicOperations.FixedTimeEquals(
    Encoding.UTF8.GetBytes(parts[0]),
    Encoding.UTF8.GetBytes(username));
bool passwordMatch = CryptographicOperations.FixedTimeEquals(
    Encoding.UTF8.GetBytes(parts[1]),
    Encoding.UTF8.GetBytes(password));
return usernameMatch && passwordMatch;
```

**Reference:** [OWASP: Timing Attacks](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)

---

### 🔴 CRITICAL — Security: Seed Data Contains Fake Password Hashes

**File:** `Data/DbSeeder.cs`, lines 21–23

```csharp
PasswordHash = "hashed_password_1",
```

**Why this matters:**  
The `PasswordHash` field is stored as plaintext strings like `"hashed_password_1"`. The `VerifyPassword` method in `PasswordHasher` splits on `.` to extract salt and hash. Calling `VerifyPassword` on a seeded user will throw an `IndexOutOfRangeException` (no `.` in the string), crashing the login flow for seeded users in any shared or staging environment.

**Suggested fix:**  
Use the real `IPasswordHasher` to hash seed passwords, or use a known pre-computed PBKDF2 hash string, or mark seed users with a distinct flag so they cannot be used to log in.

---

## 🟡 IMPORTANT Issues

---

### 🟡 IMPORTANT — Architecture: `OrderService` Bypasses Repository Layer

**File:** `Services/OrderService.cs`

`OrderService` injects and directly uses `AppDbContext` alongside `IOrderRepository`, bypassing the repository abstraction for user and product lookups and for `OrderItem` deletion:

```csharp
// Direct DbContext usage in a Service — violates architecture
User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
List<Product> products = await _context.Products.Where(...).ToListAsync();
_context.Set<OrderItem>().RemoveRange(order.Items);
```

**Why this matters:**  
The architecture rule is `Controller → Service → Repository → DbContext`. Bypassing it means the service layer cannot be tested without a real (or in-memory) database and the data access logic cannot be reused or swapped independently. It also creates two paths to the database from the same layer.

**Suggested fix:**  
Add methods to the appropriate repositories and use them:

```csharp
// IUserRepository
Task<User?> GetByEmailAsync(string email);  // already exists ✓

// IProductRepository (new)
Task<List<Product>> GetActiveByIdsAsync(List<int> ids);

// IOrderRepository (extend)
Task RemoveItemsAsync(IEnumerable<OrderItem> items);
```

Then inject `IUserRepository` and `IProductRepository` into `OrderService` and remove the `AppDbContext` dependency.

---

### 🟡 IMPORTANT — Correctness: `POST /api/orders` Returns `200 OK` Instead of `201 Created`

**File:** `Controllers/OrderController.cs`, line 40

```csharp
return Ok(result); // ❌ should be 201
```

**Why this matters:**  
The project guidelines explicitly state: `201 Created — successful POST (include created resource in body)`. Returning `200` instead of `201` violates the HTTP specification and the team's own REST conventions. Clients that inspect the status code to distinguish creation from update will behave incorrectly.

**Suggested fix:**

```csharp
return CreatedAtAction(null, result); // 201 Created
```

Same issue applies to `POST /api/users/register` in `UsersController.cs` (line 24).

---

### 🟡 IMPORTANT — Bug: Typo in Error Response Key

**File:** `Controllers/UsersController.cs`, line 42

```csharp
return BadRequest(new {meassage = ex.Message}); // ❌ "meassage"
```

**Why this matters:**  
All other error responses use `{ "message": "..." }`. This typo means the Login error body has a different key (`meassage`), breaking any client that parses the error shape, and violating the project's consistent error response shape requirement.

**Suggested fix:**

```csharp
return BadRequest(new { message = ex.Message });
```

---

### 🟡 IMPORTANT — Code Style: `var` Used in Multiple Files

The project guidelines explicitly prohibit `var`. It appears in:

| File | Location |
|---|---|
| `Services/UserService.cs` | `GenerateJwtToken` — `var jwtKey`, `var claims`, `var key`, `var credentials`, `var token` |
| `Controllers/UsersController.cs` | Login handler — `var result = await _userService.LoginAsync(dto)` |
| `Data/DbSeeder.cs` | `var users`, `var products` throughout |

**Why this matters:**  
The team has an explicit convention requiring explicit types to improve readability and code review clarity.

**Example fix (`UserService.cs`):**

```csharp
// ❌ Wrong
var jwtKey = _configuration["Jwt:Key"] ?? throw new Exception("JWT Key not configured");
var claims = new[] { ... };

// ✅ Correct
string jwtKey = _configuration["Jwt:Key"] ?? throw new Exception("JWT Key not configured");
Claim[] claims = new[] { ... };
```

---

### 🟡 IMPORTANT — Testing: Tests Don't Guard Against `Dispose` Being Skipped on Failure

**Files:** All test files in `Ecommerce.Tests/`

```csharp
AppDbContext context = TestDbContextFactory.CreateFresh();
// ...
Assert.Equal(250.00m, result.TotalPrice); // if this throws, Dispose is never called
context.Dispose(); // ❌ skipped when assertion fails
```

**Why this matters:**  
When an `Assert` fails, it throws an `XunitException`, and `context.Dispose()` on the last line is skipped. While in-memory databases are cheap, the pattern is incorrect and could leak real resources if the factory is ever changed to use a real database connection.

**Suggested fix:**  
Use the `using` declaration pattern:

```csharp
await using AppDbContext context = TestDbContextFactory.CreateFresh();
// context is always disposed, even on test failure
```

---

### 🟡 IMPORTANT — Testing: No Tests for Admin Endpoints / `ApproveOrdersAsync`

**Files:** `Ecommerce.Tests/` — no files for `AdminController` or `ApproveOrdersAsync`

**Why this matters:**  
`ApproveOrdersAsync` contains branching logic (pending check, confirmed-only filter, bulk update) that is untested. Defects in approval flow directly affect order fulfillment.

**Suggested fix:**  
Add at minimum:
- `ApproveOrdersAsync_WithConfirmedOrders_ChangesStatusToApproved`
- `ApproveOrdersAsync_WithPendingOrders_ThrowsInvalidOperationException`
- `ApproveOrdersAsync_WithUnknownOrderId_ThrowsKeyNotFoundException`

---

### 🟡 IMPORTANT — Architecture Discrepancy: SQL Server vs PostgreSQL

**File:** `Program.cs`, line 18

```csharp
options.UseSqlServer(connectionString) // ❌ uses SQL Server
```

**Why this matters:**  
The project guidelines specify **PostgreSQL** as the database. The `appsettings.json` connection string also points to `Server=db,1433` (SQL Server default port). The project has `docker-compose.yml` — if it spins up a PostgreSQL container, this will fail at startup. This discrepancy suggests the implementation diverged from the design specification.

**Suggested fix:**  
If PostgreSQL is the intended target, replace:

```csharp
options.UseSqlServer(connectionString)
```

with:

```csharp
options.UseNpgsql(connectionString)
```

And add the `Npgsql.EntityFrameworkCore.PostgreSQL` NuGet package. Verify `docker-compose.yml` matches.

---

## 🟢 SUGGESTIONS

---

### 🟢 SUGGESTION — Code Quality: Order Status as Magic Strings

**Files:** `Services/OrderService.cs`, `Services/EncryptionService.cs`, throughout

```csharp
order.Status = "Pending";
if (order.Status != "Confirmed") ...
order.Status = "Approved";
```

**Why this matters:**  
Magic strings are a source of hard-to-detect typos and make refactoring risky. Any misspelling silently produces incorrect behavior.

**Suggested fix:**

```csharp
public static class OrderStatus
{
    public const string Pending   = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Approved  = "Approved";
}

// usage
order.Status = OrderStatus.Pending;
if (order.Status != OrderStatus.Confirmed) ...
```

---

### 🟢 SUGGESTION — Code Quality: Repeated User Lookup Pattern in `OrderService`

**File:** `Services/OrderService.cs` — duplicated 3 times across `CreateOrderAsync`, `UpdateOrderAsync`, `ConfirmOrderAsync`:

```csharp
User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
if (user == null) throw new UnauthorizedAccessException("User not found");
```

**Suggested fix:**  
Extract to a private helper (or add to repository):

```csharp
private async Task<User> GetUserByEmailOrThrowAsync(string email)
{
    User? user = await _userRepository.GetByEmailAsync(email);
    return user ?? throw new UnauthorizedAccessException("User not found");
}
```

---

### 🟢 SUGGESTION — Code Quality: Typo in Interface Parameter Name

**File:** `Services/Interfaces/IOrderService.cs`, line 7

```csharp
Task<OrderResponseDto> UpdateOrderAsync(int id, UpdateOrderDto dtor, string userEmail);
//                                                              ^^^^  typo: "dtor" not "dto"
```

**Suggested fix:**

```csharp
Task<OrderResponseDto> UpdateOrderAsync(int id, UpdateOrderDto dto, string userEmail);
```

---

### 🟢 SUGGESTION — Code Quality: File Name Mismatch for `OrdersController`

The class is named `OrdersController` but the file is named `OrderController.cs`.

**Suggested fix:**  
Rename the file to `OrdersController.cs` to match the class name and follow .NET conventions.

---

### 🟢 SUGGESTION — Code Quality: Comment Remnants at End of `OrderService.cs`

**File:** `Services/OrderService.cs`, last 5 lines

```csharp
// - `userEmail` มาจาก JWT claim — ไม่รับจาก request body
// - ตรวจสอบว่า product มีอยู่จริงและ `IsActive = true` ก่อนสร้าง order
```

These are leftover development notes outside any method, after the closing `}` of the class. They should be removed or moved to the README / architecture docs.

---

### 🟢 SUGGESTION — Performance: `ApproveOrdersAsync` Saves Unchanged Orders

**File:** `Services/OrderService.cs`, `ApproveOrdersAsync`

```csharp
List<Order> confirmedOrders = orders.Where(o => o.Status == "Confirmed").ToList();
foreach (Order order in confirmedOrders) { order.Status = "Approved"; }

await _orderRepository.UpdateRangeAsync(orders); // ❌ saves ALL orders, not just confirmedOrders
```

If the input contains both `Confirmed` and `Approved` orders (which would not throw), unchanged `Approved` orders are needlessly written back to the database.

**Suggested fix:**

```csharp
await _orderRepository.UpdateRangeAsync(confirmedOrders);
```

---

### 🟢 SUGGESTION — Readability: Step Comment Numbering in `ApproveOrdersAsync` is Off

**File:** `Services/OrderService.cs`, `ApproveOrdersAsync`

The response is returned at a comment labelled `// 9. Return response`, but steps 7 and 8 are missing. This suggests the method was partially refactored. Clean up the comment numbering.

---

## Review Checklist

### Code Quality
- [x] Names are descriptive and follow naming conventions (mostly)
- [x] DTOs are properly separated from entities
- [ ] No `var` usage — **FAIL** (multiple violations in `UserService`, `UsersController`, `DbSeeder`)
- [x] Error handling is present
- [ ] No orphan comments / leftover notes — **FAIL** (`OrderService.cs` tail)
- [ ] No magic strings — **FAIL** (order statuses)
- [ ] Typo-free — **FAIL** (`meassage`, `dtor`)

### Security
- [ ] No sensitive data in source code — **FAIL** (hardcoded secrets in `appsettings.json` and `EncryptionService`)
- [x] Input validation on DTOs
- [x] No SQL injection (EF Core parameterizes queries)
- [x] Authentication correctly configured (JWT Bearer)
- [ ] Cryptography correctly implemented — **FAIL** (static IV in AES)
- [ ] Timing-safe credential comparison — **FAIL** (Basic Auth in `AdminController`)

### Testing
- [x] Core happy paths tested
- [x] Error scenarios tested (inactive product, wrong owner, insufficient stock)
- [ ] Tests use proper disposal pattern — **FAIL** (`context.Dispose()` at end, not in finally)
- [ ] Admin / approval flow tested — **FAIL** (no tests)

### Performance
- [x] Includes on queries are explicit (no N+1 on lazy load)
- [ ] `UpdateRangeAsync` operates on minimum set — **FAIL** (saves unchanged orders)

### Architecture
- [x] Controllers use only service interfaces
- [ ] Services use only repository interfaces — **FAIL** (`OrderService` uses `AppDbContext` directly)
- [x] All services and repositories registered as Scoped
- [ ] Database matches project specification (PostgreSQL vs SQL Server) — **FAIL**
- [x] Entity configurations in `Data/Configurations/`

### Documentation
- [x] Swagger configured with Bearer and Basic auth
- [ ] Seed data does not create unusable users — **FAIL** (fake password hashes)
