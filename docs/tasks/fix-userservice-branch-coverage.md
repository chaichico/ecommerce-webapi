# Fix UserService Branch Coverage

**Target:** `UserService.GenerateJwtToken` — Branch coverage 50% → 100%  
**File:** `Ecommerce.Tests/Services/UserServiceTests.cs`

---

## Root Cause

`GenerateJwtToken` มี 4 บรรทัดที่ใช้ `??` operator ซึ่งแต่ละตัวสร้าง 2 branches:

```csharp
string jwtKey           = _configuration["Jwt:Key"]             ?? throw new InvalidOperationException(...);
string jwtIssuer        = _configuration["Jwt:Issuer"]          ?? throw new InvalidOperationException(...);
string jwtAudience      = _configuration["Jwt:Audience"]        ?? throw new InvalidOperationException(...);
int jwtExpiryMinutes    = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");
```

| Branch | สถานะ | เหตุผล |
|--------|--------|--------|
| Key present | ✅ covered | test ปัจจุบัน inject config ครบ |
| Key missing → throw | ❌ missing | ยังไม่มี test |
| Issuer present | ✅ covered | " |
| Issuer missing → throw | ❌ missing | " |
| Audience present | ✅ covered | " |
| Audience missing → throw | ❌ missing | " |
| ExpiryInMinutes present | ✅ covered | " |
| ExpiryInMinutes missing → "60" | ❌ missing | " |

**รวม: 4/8 branches → 50%**

---

## Plan

### Step 1 — เพิ่ม helper method สำหรับสร้าง config บางส่วน

เพิ่ม private helper ใน `UserServiceTests` เพื่อสร้าง `UserService` ด้วย config ที่ตัด key ออก:

```csharp
private UserService BuildSutWithConfig(Dictionary<string, string?> overrides)
{
    IConfiguration cfg = new ConfigurationBuilder()
        .AddInMemoryCollection(overrides)
        .Build();

    return new UserService(
        _userRepoMock.Object,
        _passwordHasherMock.Object,
        _encryptionMock.Object,
        cfg,
        _mapperMock.Object);
}
```

---

### Step 2 — เพิ่ม test cases สำหรับ missing config keys (3 tests)

แต่ละ test ใช้ config ที่ขาด key ตัวหนึ่ง แล้วเรียก `LoginAsync` เพื่อ trigger `GenerateJwtToken`:

**Test A — Jwt:Key missing**
```csharp
[Fact]
public async Task LoginAsync_MissingJwtKey_ThrowsInvalidOperationException()
{
    UserService sut = BuildSutWithConfig(new Dictionary<string, string?>
    {
        // "Jwt:Key" ถูกตัดออก
        { "Jwt:Issuer",          "test-issuer" },
        { "Jwt:Audience",        "test-audience" },
        { "Jwt:ExpiryInMinutes", "60" }
    });

    SetupValidLoginMocks();

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => sut.LoginAsync(new LoginDto { Email = "user@example.com", Password = "correctpass" }));
}
```

**Test B — Jwt:Issuer missing**
```csharp
[Fact]
public async Task LoginAsync_MissingJwtIssuer_ThrowsInvalidOperationException()
{
    UserService sut = BuildSutWithConfig(new Dictionary<string, string?>
    {
        { "Jwt:Key",             "super-secret-key-for-testing-only-32chars!!" },
        // "Jwt:Issuer" ถูกตัดออก
        { "Jwt:Audience",        "test-audience" },
        { "Jwt:ExpiryInMinutes", "60" }
    });

    SetupValidLoginMocks();

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => sut.LoginAsync(new LoginDto { Email = "user@example.com", Password = "correctpass" }));
}
```

**Test C — Jwt:Audience missing**
```csharp
[Fact]
public async Task LoginAsync_MissingJwtAudience_ThrowsInvalidOperationException()
{
    UserService sut = BuildSutWithConfig(new Dictionary<string, string?>
    {
        { "Jwt:Key",             "super-secret-key-for-testing-only-32chars!!" },
        { "Jwt:Issuer",          "test-issuer" },
        // "Jwt:Audience" ถูกตัดออก
        { "Jwt:ExpiryInMinutes", "60" }
    });

    SetupValidLoginMocks();

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => sut.LoginAsync(new LoginDto { Email = "user@example.com", Password = "correctpass" }));
}
```

---

### Step 3 — เพิ่ม test สำหรับ ExpiryInMinutes fallback (1 test)

```csharp
[Fact]
public async Task LoginAsync_MissingExpiryInMinutes_UsesDefaultAndReturnsToken()
{
    UserService sut = BuildSutWithConfig(new Dictionary<string, string?>
    {
        { "Jwt:Key",     "super-secret-key-for-testing-only-32chars!!" },
        { "Jwt:Issuer",  "test-issuer" },
        { "Jwt:Audience","test-audience" }
        // "Jwt:ExpiryInMinutes" ถูกตัดออก → ควร fallback เป็น "60"
    });

    SetupValidLoginMocks();

    LoginResponseDto result = await sut.LoginAsync(
        new LoginDto { Email = "user@example.com", Password = "correctpass" });

    Assert.NotEmpty(result.Token);
}
```

---

### Step 4 — เพิ่ม helper `SetupValidLoginMocks()`

เพิ่ม private method เพื่อลด code ซ้ำใน test A–C และ Step 3:

```csharp
private void SetupValidLoginMocks()
{
    User user = new User
    {
        Id = 1,
        Email = "user@example.com",
        FirstName = "John",
        LastName = "Doe",
        PasswordHash = "correct_hash"
    };

    _userRepoMock
        .Setup(r => r.GetByEmail("user@example.com"))
        .ReturnsAsync(user);
    _passwordHasherMock
        .Setup(h => h.VerifyPassword("correctpass", "correct_hash"))
        .Returns(true);
    _mapperMock
        .Setup(m => m.Map<UserResponseDto>(user))
        .Returns(new UserResponseDto { Email = "user@example.com" });
}
```

---

## Expected Result

| Branch | หลัง fix |
|--------|----------|
| Key present | ✅ |
| Key missing → throw | ✅ Test A |
| Issuer present | ✅ |
| Issuer missing → throw | ✅ Test B |
| Audience present | ✅ |
| Audience missing → throw | ✅ Test C |
| ExpiryInMinutes present | ✅ |
| ExpiryInMinutes missing → "60" | ✅ Step 3 |

**Branch coverage: 8/8 → 100%**
