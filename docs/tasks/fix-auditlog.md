# Fix: Audit Log ควรมีเฉพาะ Custom Entries

## ปัญหา

`logs/audit-*.json` ในปัจจุบันมีทั้ง:
- **Framework logs** จาก ASP.NET Core / EF Core ที่ Serilog จับโดยอัตโนมัติ
  (เช่น `Request starting`, `Executed DbCommand`, `Route matched`, `Storing keys in a directory`)
- **Custom entries** ที่ `LogBackgroundService` เขียนผ่าน `_logger.LogInformation(...)`

สาเหตุ: `builder.Host.UseSerilog()` ใน `Program.cs` แทนที่ logging provider ทั้งหมด
ทำให้ log ทุก source รวมทั้ง framework ถูกส่งไปยัง Serilog sink เดียว (ไฟล์ audit)

ผลลัพธ์คือ audit log ไม่ตรงกับ model `LogEntry` ที่ออกแบบไว้
และเต็มไปด้วย noise ที่ไม่ต้องการ

---

## เป้าหมาย

audit file ต้องมีเฉพาะ entries ที่ตรงกับ `LogEntry`:

```json
{
  "Timestamp": "...",
  "ActionName": "API.Orders.CreateOrder",
  "Method": "POST",
  "Path": "/api/Orders",
  "StatusCode": 201,
  "TraceId": "...",
  "IpAddress": "...",
  "UserName": "user@example.com",
  "ElapsedMs": 144
}
```

---

## แนวทางแก้ไข (แนะนำ): เขียน audit file โดยตรงจาก LogBackgroundService

แทนที่จะใช้ `_logger.LogInformation(...)` (ซึ่งส่ง log กลับเข้า Serilog pipeline รวมกับ framework logs)
ให้ `LogBackgroundService` เขียน `LogEntry` โดยตรงเป็น JSON ลงไฟล์ด้วย `System.Text.Json`

### ขั้นตอน

#### 1. แก้ `LogBackgroundService` — ลบ ILogger ออก เขียนไฟล์ตรง [x]

```csharp
// Logging/LogBackgroundService.cs
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace Logging;

public class LogBackgroundService : BackgroundService
{
    private readonly ILogChannel _channel;
    private readonly string _auditPath;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    public LogBackgroundService(ILogChannel channel, IHostEnvironment env)
    {
        _channel = channel;
        // เขียนลง logs/ directory เดิม, rolling by date
        string dir = Path.Combine(env.ContentRootPath, "logs");
        Directory.CreateDirectory(dir);
        _auditPath = Path.Combine(dir, $"audit-{DateOnly.FromDateTime(DateTime.UtcNow):yyyyMMdd}.json");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (LogEntry entry in _channel.ReadAllAsync(stoppingToken))
        {
            string line = JsonSerializer.Serialize(entry, _jsonOptions);
            await File.AppendAllTextAsync(_auditPath, line + Environment.NewLine, stoppingToken);
        }
    }
}
```

> **หมายเหตุ:** หาก traffic สูง ควรใช้ `StreamWriter` แบบ persistent แทน `File.AppendAllTextAsync`
> เพื่อลด overhead ของการเปิด/ปิด file handle ทุก entry

#### 2. แก้ Serilog ใน `Program.cs` — ให้ log ไปที่ app log แทน audit [x]

```csharp
// Program.cs — เปลี่ยนชื่อ path ให้ชัดเจนว่าเป็น app log ไม่ใช่ audit
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .MinimumLevel.Warning()                          // กรอง framework noise ออก
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Async(a => a.File(
        path: "logs/app-.json",                      // เปลี่ยนจาก audit- → app-
        formatter: new Serilog.Formatting.Json.JsonFormatter(),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30))
    .CreateLogger();
```

#### 3. ลบ ILogger dependency ออกจาก LogBackgroundService registration [x]

เนื่องจาก `LogBackgroundService` ไม่ใช้ `ILogger` แล้ว ไม่ต้องเปลี่ยนอะไรใน `Program.cs`
(DI inject `ILogChannel` และ `IHostEnvironment` ได้อัตโนมัติ)

---

## แนวทางทางเลือก: ใช้ Serilog Filter

หากต้องการคงการใช้ `ILogger` ใน `LogBackgroundService` ไว้
สามารถกรองเฉพาะ SourceContext ที่ต้องการให้เข้า audit sink:

```csharp
.WriteTo.Async(a => a.File(
    path: "logs/audit-.json",
    formatter: new Serilog.Formatting.Json.JsonFormatter(),
    rollingInterval: RollingInterval.Day),
    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
.Filter.ByIncludingOnly(e =>
    e.Properties.TryGetValue("SourceContext", out Serilog.Events.LogEventPropertyValue? v)
    && v.ToString().Contains("Logging.LogBackgroundService"))
```

**ข้อเสีย:** audit file ยังเป็น Serilog format (มี `MessageTemplate`, `Properties` object)
ไม่ใช่ `LogEntry` JSON ตรง ๆ — ต้องแปลงเพิ่มเติมถ้าต้องการ query ภายหลัง

---

## ผลลัพธ์ที่ได้หลังแก้ไข

| ไฟล์ | เนื้อหา |
|------|---------|
| `logs/audit-YYYYMMDD.json` | เฉพาะ `LogEntry` custom entries (1 entry ต่อ request) |
| `logs/app-YYYYMMDD.json` | Framework logs จาก ASP.NET Core / EF Core (Warning ขึ้นไป) |
