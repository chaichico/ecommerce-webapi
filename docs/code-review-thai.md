# Code Review — Ecommerce API (ฉบับภาษาไทย)

**วันที่:** 28 เมษายน 2026  
**ผู้รีวิว:** GitHub Copilot  
**Tech Stack:** .NET 10 Web API · Entity Framework Core · SQL Server · JWT Bearer Authentication  
**ขอบเขต:** รีวิว codebase ทั้งหมด — ความปลอดภัย, สถาปัตยกรรม, คุณภาพโค้ด, การทดสอบ, ประสิทธิภาพ

---

## สรุปภาพรวม

Codebase มีสถาปัตยกรรมแบบ layered ที่ชัดเจนและแสดงให้เห็นการออกแบบ REST API ที่ดี มีการใช้ JWT authentication, password hashing และ DTO patterns อย่างถูกต้อง อย่างไรก็ตาม มี **ปัญหาด้านความปลอดภัยระดับวิกฤต** (secret ที่ hardcode ไว้, static IV ใน AES), **การละเมิดสถาปัตยกรรม** (service ข้ามชั้น repository layer), และ **bugs สำคัญหลายจุด** (typo ในการตอบกลับข้อผิดพลาด, HTTP status code ไม่ถูกต้องในการสร้างข้อมูล, การใช้ var) ที่ต้องแก้ไขก่อน deploy ขึ้น production

---

## 🔴 ปัญหาระดับวิกฤต (CRITICAL)

---

### 🔴 วิกฤต — ความปลอดภัย: Secret ถูก Hardcode ใน `appsettings.json`

**ไฟล์:** `appsettings.json`

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

นอกจากนี้ยังพบ admin credential ซ้ำสองชุด (`AdminCredentials` และ `AdminAuth`) โดยชุดหนึ่งดูเหมือนจะเป็น dead code

**ทำไมถึงสำคัญ:**  
Secret ที่ commit เข้า source control สามารถดึงออกมาจาก git history ได้แม้จะลบออกไปแล้ว JWT key คือ signing secret ของ token ผู้ใช้ทุกคน encryption key ปกป้องข้อมูล PII (เบอร์โทรศัพท์) นักพัฒนาหรือระบบ CI/CD ที่เข้าถึง repository ได้ สามารถแอบอ้างตัวเป็นผู้ใช้หรือถอดรหัสข้อมูล PII ที่จัดเก็บไว้

**วิธีแก้ไข:**  
ย้าย secret ทั้งหมดไปไว้ใน environment variables หรือ secrets manager ใช้ค่าว่างใน `appsettings.json` และระบุ environment variables ที่จำเป็นไว้ใน README:

```json
"Jwt": { "Key": "" },
"Encryption": { "Key": "", "IV": "" },
"AdminAuth": { "Username": "", "Password": "" }
```

ตั้งค่าผ่าน environment variables หรือ `dotnet user-secrets` บนเครื่องท้องถิ่น:
```bash
dotnet user-secrets set "Jwt:Key" "<random-256-bit-value>"
```

ลบ block `AdminCredentials` ที่เป็น dead code ออกทั้งหมด

**อ้างอิง:** [OWASP: Sensitive Data Exposure](https://owasp.org/Top10/A02_2021-Cryptographic_Failures/)

---

### 🔴 วิกฤต — ความปลอดภัย: AES IV แบบ Static ใน `EncryptionService.cs`

**ไฟล์:** `Services/EncryptionService.cs`, บรรทัดที่ 8–20

```csharp
string ivString = configuration["Encryption:IV"] ?? "YourIV1234567890"; // 16 ตัวอักษร
_iv = Encoding.UTF8.GetBytes(ivString);
// ...
aes.IV = _iv; // ใช้ IV เดิมทุกครั้งที่เข้ารหัส
```

**ทำไมถึงสำคัญ:**  
AES-CBC ที่ใช้ IV ตายตัวจะเปิดเผย pattern: ผู้ใช้สองคนที่มีเบอร์โทรศัพท์เดียวกันจะได้ ciphertext ที่เหมือนกัน ซึ่งทำให้การเข้ารหัสข้อมูล PII ไม่มีประสิทธิภาพโดยสิ้นเชิง นอกจากนี้ fallback `?? "YourIV1234567890"` ยังฝัง secret ไว้ใน source code อีกด้วย

**วิธีแก้ไข:**  
สร้าง IV แบบสุ่มใหม่ทุกครั้งที่เข้ารหัส, แนบไว้ต้น ciphertext, และอ่านกลับมาตอนถอดรหัส:

```csharp
public string Encrypt(string plainText)
{
    using Aes aes = Aes.Create();
    aes.Key = _key;
    aes.GenerateIV(); // สร้าง IV แบบสุ่มใหม่ทุกครั้ง

    ICryptoTransform encryptor = aes.CreateEncryptor();
    using MemoryStream ms = new MemoryStream();
    ms.Write(aes.IV, 0, aes.IV.Length); // แนบ IV ไว้ต้น (16 bytes)
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

ลบ key `IV` ออกจาก configuration ทั้งหมด เนื่องจากไม่จำเป็นอีกต่อไป

**อ้างอิง:** [NIST SP 800-38A — AES-CBC Mode](https://csrc.nist.gov/publications/detail/sp/800-38a/final)

---

### 🔴 วิกฤต — ความปลอดภัย: Secret Fallback แบบ Hardcode ใน `EncryptionService.cs`

**ไฟล์:** `Services/EncryptionService.cs`, บรรทัดที่ 14–15

```csharp
string keyString = configuration["Encryption:Key"] ?? "YourSecretKey1234567890123456";
string ivString  = configuration["Encryption:IV"]  ?? "YourIV1234567890";
```

**ทำไมถึงสำคัญ:**  
หาก configuration key หายไป (เช่น ตั้งค่า environment ผิด) โค้ดจะ fallback ไปใช้ secret ที่ hardcode ไว้โดยไม่มีการแจ้งเตือน ซึ่งหมายความว่าการตั้งค่าผิดพลาดจะยังคงให้ความรู้สึกปลอดภัยแบบหลอกๆ — ข้อมูลถูก "เข้ารหัส" แต่ใครก็ตามที่อ่าน source code จะถอดรหัสได้ทันที

**วิธีแก้ไข:**  
โยน exception หาก key ไม่ได้ถูกตั้งค่า ตามแบบที่ใช้ใน `UserService.GenerateJwtToken` อยู่แล้ว:

```csharp
string keyString = configuration["Encryption:Key"]
    ?? throw new InvalidOperationException("Encryption:Key is not configured");
```

---

### 🔴 วิกฤต — ความปลอดภัย: ความเสี่ยง Timing Attack ใน `AdminController.IsAuthorized()`

**ไฟล์:** `Controllers/AdminController.cs`, บรรทัดที่ 36–37

```csharp
return parts[0] == username && parts[1] == password;
```

**ทำไมถึงสำคัญ:**  
การเปรียบเทียบ string ธรรมดา (`==`) จะหยุดทันทีที่พบ byte แรกที่ไม่ตรงกัน ทำให้เวลาตอบสนองขึ้นอยู่กับจำนวนตัวอักษรที่ตรงกันตั้งแต่ต้น ผู้โจมตีสามารถวัดเวลาตอบสนองเพื่อ brute-force admin credentials ได้ทีละตัวอักษร

**วิธีแก้ไข:**  
ใช้การเปรียบเทียบแบบ constant-time:

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

**อ้างอิง:** [OWASP: Timing Attacks](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)

---

### 🔴 วิกฤต — ความปลอดภัย: Seed Data มี Password Hash ปลอม

**ไฟล์:** `Data/DbSeeder.cs`, บรรทัดที่ 21–23

```csharp
PasswordHash = "hashed_password_1",
```

**ทำไมถึงสำคัญ:**  
ฟิลด์ `PasswordHash` ถูกเก็บเป็น string ธรรมดาเช่น `"hashed_password_1"` method `VerifyPassword` ใน `PasswordHasher` แยกค่าด้วย `.` เพื่อดึง salt และ hash การเรียก `VerifyPassword` กับผู้ใช้ที่ถูก seed จะโยน `IndexOutOfRangeException` (ไม่มี `.` ใน string) ทำให้ login flow สำหรับผู้ใช้ที่ถูก seed ในทุก shared หรือ staging environment พัง

**วิธีแก้ไข:**  
ใช้ `IPasswordHasher` จริงในการ hash รหัสผ่านตอน seed หรือใช้ string PBKDF2 hash ที่คำนวณไว้ล่วงหน้า หรือทำเครื่องหมาย seed users ด้วย flag พิเศษเพื่อป้องกันการใช้งาน login

---

## 🟡 ปัญหาระดับสำคัญ (IMPORTANT)

---

### 🟡 สำคัญ — สถาปัตยกรรม: `OrderService` ข้ามชั้น Repository Layer

**ไฟล์:** `Services/OrderService.cs`

`OrderService` inject และใช้ `AppDbContext` โดยตรงควบคู่กับ `IOrderRepository` โดยข้าม repository abstraction สำหรับการค้นหา user และ product และการลบ `OrderItem`:

```csharp
// ใช้ DbContext โดยตรงใน Service — ละเมิดสถาปัตยกรรม
User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
List<Product> products = await _context.Products.Where(...).ToListAsync();
_context.Set<OrderItem>().RemoveRange(order.Items);
```

**ทำไมถึงสำคัญ:**  
กฎสถาปัตยกรรมคือ `Controller → Service → Repository → DbContext` การข้ามชั้นทำให้ service layer ไม่สามารถทดสอบได้โดยไม่ต้องใช้ฐานข้อมูลจริง (หรือ in-memory) และ data access logic ไม่สามารถ reuse หรือเปลี่ยนแปลงได้อย่างอิสระ นอกจากนี้ยังสร้างสองเส้นทางสู่ฐานข้อมูลจาก layer เดียวกัน

**วิธีแก้ไข:**  
เพิ่ม methods ใน repositories ที่เหมาะสมและใช้งาน:

```csharp
// IUserRepository
Task<User?> GetByEmailAsync(string email);  // มีอยู่แล้ว ✓

// IProductRepository (ใหม่)
Task<List<Product>> GetActiveByIdsAsync(List<int> ids);

// IOrderRepository (เพิ่มเติม)
Task RemoveItemsAsync(IEnumerable<OrderItem> items);
```

จากนั้น inject `IUserRepository` และ `IProductRepository` เข้าไปใน `OrderService` และลบ dependency ของ `AppDbContext` ออก

---

### 🟡 สำคัญ — ความถูกต้อง: `POST /api/orders` คืนค่า `200 OK` แทน `201 Created`

**ไฟล์:** `Controllers/OrderController.cs`, บรรทัดที่ 40

```csharp
return Ok(result); // ❌ ควรเป็น 201
```

**ทำไมถึงสำคัญ:**  
แนวทางของโปรเจกต์ระบุชัดเจนว่า: `201 Created — POST ที่สำเร็จ (แนบ resource ที่สร้างไว้ใน body)` การคืนค่า `200` แทน `201` ละเมิด HTTP specification และ REST conventions ของทีม client ที่ตรวจสอบ status code เพื่อแยกแยะการสร้างจากการอัปเดตจะทำงานผิดพลาด

**วิธีแก้ไข:**

```csharp
return CreatedAtAction(null, result); // 201 Created
```

ปัญหาเดียวกันเกิดขึ้นกับ `POST /api/users/register` ใน `UsersController.cs` (บรรทัดที่ 24) ด้วย

---

### 🟡 สำคัญ — Bug: Typo ใน key ของ Error Response

**ไฟล์:** `Controllers/UsersController.cs`, บรรทัดที่ 42

```csharp
return BadRequest(new {meassage = ex.Message}); // ❌ "meassage"
```

**ทำไมถึงสำคัญ:**  
error response อื่นๆ ทั้งหมดใช้ `{ "message": "..." }` การพิมพ์ผิดนี้ทำให้ error body ของ Login มี key ที่ต่างออกไป (`meassage`) ซึ่งทำลาย client ใดก็ตามที่ parse รูปแบบ error และละเมิดรูปแบบ error response ที่สม่ำเสมอของโปรเจกต์

**วิธีแก้ไข:**

```csharp
return BadRequest(new { message = ex.Message });
```

---

### 🟡 สำคัญ — Code Style: ใช้ `var` ในหลายไฟล์

แนวทางของโปรเจกต์ห้ามใช้ `var` อย่างชัดเจน พบการใช้งานใน:

| ไฟล์ | ตำแหน่ง |
|---|---|
| `Services/UserService.cs` | `GenerateJwtToken` — `var jwtKey`, `var claims`, `var key`, `var credentials`, `var token` |
| `Controllers/UsersController.cs` | Login handler — `var result = await _userService.LoginAsync(dto)` |
| `Data/DbSeeder.cs` | `var users`, `var products` ตลอดทั้งไฟล์ |

**ทำไมถึงสำคัญ:**  
ทีมมี convention ที่ชัดเจนในการกำหนด type แบบ explicit เพื่อเพิ่มความชัดเจนในการอ่านและ code review

**ตัวอย่างการแก้ไข (`UserService.cs`):**

```csharp
// ❌ ผิด
var jwtKey = _configuration["Jwt:Key"] ?? throw new Exception("JWT Key not configured");
var claims = new[] { ... };

// ✅ ถูก
string jwtKey = _configuration["Jwt:Key"] ?? throw new Exception("JWT Key not configured");
Claim[] claims = new[] { ... };
```

---

### 🟡 สำคัญ — การทดสอบ: Tests ไม่รับประกันว่า `Dispose` จะถูกเรียกเมื่อเกิดข้อผิดพลาด

**ไฟล์:** ไฟล์ test ทั้งหมดใน `Ecommerce.Tests/`

```csharp
AppDbContext context = TestDbContextFactory.CreateFresh();
// ...
Assert.Equal(250.00m, result.TotalPrice); // ถ้า throw, Dispose จะไม่ถูกเรียก
context.Dispose(); // ❌ ถูกข้ามเมื่อ assertion ล้มเหลว
```

**ทำไมถึงสำคัญ:**  
เมื่อ `Assert` ล้มเหลว มันจะโยน `XunitException` และ `context.Dispose()` ในบรรทัดสุดท้ายจะถูกข้าม แม้ in-memory database จะมีราคาถูก แต่ pattern นี้ไม่ถูกต้องและอาจทำให้ resource รั่วได้หาก factory ถูกเปลี่ยนให้ใช้ database connection จริง

**วิธีแก้ไข:**  
ใช้ pattern `using` declaration:

```csharp
await using AppDbContext context = TestDbContextFactory.CreateFresh();
// context จะถูก dispose เสมอ แม้ test จะล้มเหลว
```

---

### 🟡 สำคัญ — การทดสอบ: ไม่มี Tests สำหรับ Admin Endpoints / `ApproveOrdersAsync`

**ไฟล์:** `Ecommerce.Tests/` — ไม่มีไฟล์สำหรับ `AdminController` หรือ `ApproveOrdersAsync`

**ทำไมถึงสำคัญ:**  
`ApproveOrdersAsync` มี branching logic (ตรวจสอบ pending, กรอง confirmed-only, bulk update) ที่ไม่ได้ถูกทดสอบ ข้อบกพร่องใน approval flow ส่งผลโดยตรงต่อการจัดการ order

**วิธีแก้ไข:**  
เพิ่ม test อย่างน้อย:
- `ApproveOrdersAsync_WithConfirmedOrders_ChangesStatusToApproved`
- `ApproveOrdersAsync_WithPendingOrders_ThrowsInvalidOperationException`
- `ApproveOrdersAsync_WithUnknownOrderId_ThrowsKeyNotFoundException`

---

### 🟡 สำคัญ — ความไม่สอดคล้องทางสถาปัตยกรรม: SQL Server vs PostgreSQL

**ไฟล์:** `Program.cs`, บรรทัดที่ 18

```csharp
options.UseSqlServer(connectionString) // ❌ ใช้ SQL Server
```

**ทำไมถึงสำคัญ:**  
แนวทางโปรเจกต์ระบุ **PostgreSQL** เป็นฐานข้อมูล connection string ใน `appsettings.json` ยังชี้ไปที่ `Server=db,1433` (port เริ่มต้นของ SQL Server) โปรเจกต์มี `docker-compose.yml` — ถ้ามันสร้าง PostgreSQL container, จะเกิดข้อผิดพลาดตั้งแต่เริ่มต้น ความไม่สอดคล้องนี้แสดงว่า implementation เบี่ยงออกจาก design specification

**วิธีแก้ไข:**  
ถ้า PostgreSQL คือเป้าหมายที่ต้องการ ให้แทนที่:

```csharp
options.UseSqlServer(connectionString)
```

ด้วย:

```csharp
options.UseNpgsql(connectionString)
```

และเพิ่ม NuGet package `Npgsql.EntityFrameworkCore.PostgreSQL` ตรวจสอบว่า `docker-compose.yml` ตรงกัน

---

## 🟢 ข้อแนะนำ (SUGGESTIONS)

---

### 🟢 แนะนำ — คุณภาพโค้ด: Order Status เป็น Magic Strings

**ไฟล์:** `Services/OrderService.cs`, `Services/EncryptionService.cs` และทั่วไป

```csharp
order.Status = "Pending";
if (order.Status != "Confirmed") ...
order.Status = "Approved";
```

**ทำไมถึงสำคัญ:**  
Magic strings เป็นแหล่งของการพิมพ์ผิดที่ตรวจพบยากและทำให้การ refactor มีความเสี่ยง การพิมพ์ผิดใดๆ จะสร้างพฤติกรรมที่ผิดพลาดโดยไม่มีการแจ้งเตือน

**วิธีแก้ไข:**

```csharp
public static class OrderStatus
{
    public const string Pending   = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Approved  = "Approved";
}

// การใช้งาน
order.Status = OrderStatus.Pending;
if (order.Status != OrderStatus.Confirmed) ...
```

---

### 🟢 แนะนำ — คุณภาพโค้ด: Pattern การค้นหา User ซ้ำกันใน `OrderService`

**ไฟล์:** `Services/OrderService.cs` — ซ้ำกัน 3 ครั้งใน `CreateOrderAsync`, `UpdateOrderAsync`, `ConfirmOrderAsync`:

```csharp
User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
if (user == null) throw new UnauthorizedAccessException("User not found");
```

**วิธีแก้ไข:**  
แยกเป็น private helper (หรือเพิ่มใน repository):

```csharp
private async Task<User> GetUserByEmailOrThrowAsync(string email)
{
    User? user = await _userRepository.GetByEmailAsync(email);
    return user ?? throw new UnauthorizedAccessException("User not found");
}
```

---

### 🟢 แนะนำ — คุณภาพโค้ด: Typo ในชื่อ Parameter ของ Interface

**ไฟล์:** `Services/Interfaces/IOrderService.cs`, บรรทัดที่ 7

```csharp
Task<OrderResponseDto> UpdateOrderAsync(int id, UpdateOrderDto dtor, string userEmail);
//                                                              ^^^^  typo: "dtor" ไม่ใช่ "dto"
```

**วิธีแก้ไข:**

```csharp
Task<OrderResponseDto> UpdateOrderAsync(int id, UpdateOrderDto dto, string userEmail);
```

---

### 🟢 แนะนำ — คุณภาพโค้ด: ชื่อไฟล์ไม่ตรงกับ `OrdersController`

Class มีชื่อว่า `OrdersController` แต่ไฟล์มีชื่อว่า `OrderController.cs`

**วิธีแก้ไข:**  
เปลี่ยนชื่อไฟล์เป็น `OrdersController.cs` ให้ตรงกับชื่อ class และปฏิบัติตาม .NET conventions

---

### 🟢 แนะนำ — คุณภาพโค้ด: Comment เหลือทิ้งท้าย `OrderService.cs`

**ไฟล์:** `Services/OrderService.cs`, 5 บรรทัดสุดท้าย

```csharp
// - `userEmail` มาจาก JWT claim — ไม่รับจาก request body
// - ตรวจสอบว่า product มีอยู่จริงและ `IsActive = true` ก่อนสร้าง order
```

นี่คือ development notes ที่หลงเหลืออยู่นอก method ใดๆ หลังจาก `}` ปิดท้าย class ควรลบออกหรือย้ายไปไว้ใน README / architecture docs

---

### 🟢 แนะนำ — ประสิทธิภาพ: `ApproveOrdersAsync` บันทึก Order ที่ไม่มีการเปลี่ยนแปลง

**ไฟล์:** `Services/OrderService.cs`, `ApproveOrdersAsync`

```csharp
List<Order> confirmedOrders = orders.Where(o => o.Status == "Confirmed").ToList();
foreach (Order order in confirmedOrders) { order.Status = "Approved"; }

await _orderRepository.UpdateRangeAsync(orders); // ❌ บันทึก orders ทั้งหมด ไม่ใช่แค่ confirmedOrders
```

ถ้า input มีทั้ง order สถานะ `Confirmed` และ `Approved` (ซึ่งจะไม่ throw exception) order ที่สถานะ `Approved` ที่ไม่มีการเปลี่ยนแปลงก็จะถูกเขียนกลับไปยังฐานข้อมูลโดยไม่จำเป็น

**วิธีแก้ไข:**

```csharp
await _orderRepository.UpdateRangeAsync(confirmedOrders);
```

---

### 🟢 แนะนำ — ความอ่านง่าย: หมายเลข Step Comment ใน `ApproveOrdersAsync` ผิดลำดับ

**ไฟล์:** `Services/OrderService.cs`, `ApproveOrdersAsync`

การ return ค่าอยู่ที่ comment ที่ระบุว่า `// 9. Return response` แต่ขาด step 7 และ 8 ซึ่งแสดงว่า method ถูก refactor บางส่วน ควรจัดการหมายเลข comment ให้เรียบร้อย

---

## Checklist การ Review

### คุณภาพโค้ด
- [x] ชื่อสื่อความหมายและปฏิบัติตาม naming conventions (ส่วนใหญ่)
- [x] DTOs แยกออกจาก entities อย่างถูกต้อง
- [ ] ไม่ใช้ `var` — **ไม่ผ่าน** (ละเมิดหลายจุดใน `UserService`, `UsersController`, `DbSeeder`)
- [x] มีการจัดการข้อผิดพลาด
- [ ] ไม่มี orphan comments / development notes เหลือทิ้ง — **ไม่ผ่าน** (ปลายไฟล์ `OrderService.cs`)
- [ ] ไม่มี magic strings — **ไม่ผ่าน** (order statuses)
- [ ] ไม่มี typo — **ไม่ผ่าน** (`meassage`, `dtor`)

### ความปลอดภัย
- [ ] ไม่มีข้อมูลละเอียดอ่อนใน source code — **ไม่ผ่าน** (hardcoded secrets ใน `appsettings.json` และ `EncryptionService`)
- [x] มีการ validate input บน DTOs
- [x] ไม่มี SQL injection (EF Core parameterize queries)
- [x] ตั้งค่า Authentication ถูกต้อง (JWT Bearer)
- [ ] Cryptography ถูกต้อง — **ไม่ผ่าน** (static IV ใน AES)
- [ ] Credential comparison แบบ timing-safe — **ไม่ผ่าน** (Basic Auth ใน `AdminController`)

### การทดสอบ
- [x] ทดสอบ happy path หลักๆ แล้ว
- [x] ทดสอบ error scenarios แล้ว (product ไม่ active, เจ้าของผิด, stock ไม่พอ)
- [ ] Tests ใช้ pattern การ dispose ที่ถูกต้อง — **ไม่ผ่าน** (`context.Dispose()` ท้ายสุด ไม่อยู่ใน finally)
- [ ] ทดสอบ Admin / approval flow แล้ว — **ไม่ผ่าน** (ไม่มี tests)

### ประสิทธิภาพ
- [x] Includes บน queries เป็นแบบ explicit (ไม่มี N+1 จาก lazy load)
- [ ] `UpdateRangeAsync` ทำงานกับ set ขั้นต่ำ — **ไม่ผ่าน** (บันทึก orders ที่ไม่มีการเปลี่ยนแปลง)

### สถาปัตยกรรม
- [x] Controllers ใช้เฉพาะ service interfaces
- [ ] Services ใช้เฉพาะ repository interfaces — **ไม่ผ่าน** (`OrderService` ใช้ `AppDbContext` โดยตรง)
- [x] Services และ repositories ทั้งหมด registered เป็น Scoped
- [ ] Database ตรงกับ specification ของโปรเจกต์ (PostgreSQL vs SQL Server) — **ไม่ผ่าน**
- [x] Entity configurations อยู่ใน `Data/Configurations/`

### เอกสาร
- [x] Swagger ตั้งค่าพร้อม Bearer และ Basic auth
- [ ] Seed data ไม่สร้าง users ที่ใช้งานไม่ได้ — **ไม่ผ่าน** (fake password hashes)
