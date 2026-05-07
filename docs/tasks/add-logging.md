# Logging Implementation Guide

แนวทางการเพิ่มระบบ High-Performance Logging สำหรับ Ecommerce API (.NET 10)

---

## Architecture Overview

```
HTTP Request
    |
    v
LoggingMiddleware  ← ดักจับ Request/Response ทุกตัว (non-blocking)
    |
    +──► Channel<LogEntry>  ──► LogBackgroundService ──► Serilog ──► logs/audit.json
    |
    +──► ConcurrentDictionary (นับ in-memory)
    |           |
    |     PeriodicTimer ──► Flush ──► logs/summary.json
    |
    v
API Response (กลับทันที ไม่ถูก block)
```

ใช้ **2 Path** แยกกัน:
- **Audit Path** — บันทึกทุก Request/Response เป็น JSON ลงไฟล์ (รองรับ Compliance)
- **Summary Path** — สรุปสถิติ in-memory แล้ว flush ทุก 5 นาที

---

## NuGet Packages ที่ต้องติดตั้ง

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Async
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread
```

---

## โครงสร้างไฟล์ที่ต้องสร้าง

```
Logging/
    LogEntry.cs                  ← Model สำหรับ log entry
    ILogChannel.cs               ← Interface ตาม convention โปรเจกต์
    LogChannel.cs                ← Wrapper สำหรับ Channel<LogEntry>
    LogBackgroundService.cs      ← BackgroundService (consumer)
    LogSummaryService.cs         ← PeriodicTimer + ConcurrentDictionary
Middleware/
    LoggingMiddleware.cs         ← ดักจับ Request/Response
```

---

## ขั้นตอนการ Implement

### Step 1 — สร้าง `LogEntry` Model

```csharp
// Logging/LogEntry.cs
namespace Logging;

public class LogEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string Method { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public string? UserName { get; init; }
    public string? IpAddress { get; init; }
    public long ElapsedMs { get; init; }
    public string? TraceId { get; init; }
}
```

---

### Step 2 — สร้าง `ILogChannel` Interface และ `LogChannel`

```csharp
// Logging/ILogChannel.cs
namespace Logging;

public interface ILogChannel
{
    ValueTask WriteAsync(LogEntry entry, CancellationToken ct = default);
    IAsyncEnumerable<LogEntry> ReadAllAsync(CancellationToken ct = default);
}
```

```csharp
// Logging/LogChannel.cs
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Logging;

public class LogChannel : ILogChannel
{
    private readonly Channel<LogEntry> _channel;

    public LogChannel()
    {
        _channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(2048)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
    }

    public ValueTask WriteAsync(LogEntry entry, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(entry, ct);

    public async IAsyncEnumerable<LogEntry> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (LogEntry entry in _channel.Reader.ReadAllAsync(ct))
        {
            yield return entry;
        }
    }
}
```

> **หมายเหตุ:** `BoundedCapacity = 2048` และ `DropOldest` ป้องกัน memory ล้นเมื่อ consumer ช้า

---

### Step 3 — สร้าง `LogBackgroundService` (Serilog Consumer)

```csharp
// Logging/LogBackgroundService.cs
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Logging;

public class LogBackgroundService : BackgroundService
{
    private readonly ILogChannel _channel;
    private readonly ILogger _logger;

    public LogBackgroundService(ILogChannel channel, ILogger logger)
    {
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (LogEntry entry in _channel.ReadAllAsync(stoppingToken))
        {
            _logger
                .ForContext("TraceId", entry.TraceId)
                .ForContext("IpAddress", entry.IpAddress)
                .ForContext("UserName", entry.UserName)
                .ForContext("ElapsedMs", entry.ElapsedMs)
                .Information(
                    "{Method} {Path} → {StatusCode}",
                    entry.Method, entry.Path, entry.StatusCode);
        }
    }
}
```

---

### Step 4 — สร้าง `LogSummaryService` (Stats + PeriodicTimer)

```csharp
// Logging/LogSummaryService.cs
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Logging;

public class LogSummaryService : BackgroundService
{
    private readonly ConcurrentDictionary<string, int> _counts = new();
    private readonly ILogger<LogSummaryService> _logger;

    public LogSummaryService(ILogger<LogSummaryService> logger)
    {
        _logger = logger;
    }

    public void Increment(string key) => _counts.AddOrUpdate(key, 1, (_, v) => v + 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await FlushAsync();
        }
    }

    private async Task FlushAsync()
    {
        Dictionary<string, int> snapshot = new(_counts);
        string json = JsonSerializer.Serialize(new
        {
            Timestamp = DateTimeOffset.UtcNow,
            Stats = snapshot
        });

        string path = Path.Combine("logs", $"summary-{DateTime.UtcNow:yyyyMMdd}.json");
        Directory.CreateDirectory("logs");
        await File.AppendAllTextAsync(path, json + Environment.NewLine);
        _logger.LogInformation("Summary flushed: {Count} keys", snapshot.Count);
    }
}
```

---

### Step 5 — สร้าง `LoggingMiddleware`

```csharp
// Middleware/LoggingMiddleware.cs
using System.Diagnostics;
using System.Security.Claims;
using Logging;

namespace Middleware;

public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogChannel _channel;
    private readonly LogSummaryService _summary;

    public LoggingMiddleware(
        RequestDelegate next,
        ILogChannel channel,
        LogSummaryService summary)
    {
        _next = next;
        _channel = channel;
        _summary = summary;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Stopwatch sw = Stopwatch.StartNew();

        await _next(context);

        sw.Stop();

        string? userName = context.User.FindFirst(ClaimTypes.Name)?.Value
                        ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        LogEntry entry = new()
        {
            Timestamp   = DateTimeOffset.UtcNow,
            Method      = context.Request.Method,
            Path        = context.Request.Path,
            StatusCode  = context.Response.StatusCode,
            UserName    = userName,
            IpAddress   = context.Connection.RemoteIpAddress?.ToString(),
            ElapsedMs   = sw.ElapsedMilliseconds,
            TraceId     = context.TraceIdentifier
        };

        // Non-blocking write
        await _channel.WriteAsync(entry);

        // In-memory stats
        string key = $"{context.Request.Method}:{context.Response.StatusCode}";
        _summary.Increment(key);
    }
}
```

---

### Step 6 — Configure Serilog ใน `Program.cs`

```csharp
// Program.cs — เพิ่มก่อน builder.Build()

using Serilog;
using Logging;
using Middleware;

// Serilog setup
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Async(a => a.File(
        path: "logs/audit-.json",
        formatter: new Serilog.Formatting.Json.JsonFormatter(),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30))
    .CreateLogger();

builder.Host.UseSerilog();

// Register Logging Services
builder.Services.AddSingleton<ILogChannel, LogChannel>();
builder.Services.AddSingleton<LogSummaryService>();
builder.Services.AddHostedService<LogBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<LogSummaryService>());
```

```csharp
// Program.cs — เพิ่มหลัง app.Build() ก่อน middleware อื่น

app.UseMiddleware<LoggingMiddleware>();
```

> **ลำดับ Middleware สำคัญ** — `LoggingMiddleware` ต้องอยู่ก่อน `UseAuthentication` และ `UseAuthorization`

---

### Step 7 — สร้างโฟลเดอร์ `logs/` และ gitignore

```bash
mkdir logs
echo "logs/" >> .gitignore
```

---

## Checklist

### Setup
- [x] ติดตั้ง NuGet packages ทั้ง 5 ตัว (`Serilog.AspNetCore`, `Sinks.File`, `Sinks.Async`, `Enrichers.Environment`, `Enrichers.Thread`)
- [x] สร้างโฟลเดอร์ `Logging/` ที่ root ของโปรเจกต์
- [x] สร้างโฟลเดอร์ `Middleware/` ที่ root ของโปรเจกต์ (ถ้ายังไม่มี)
- [x] เพิ่ม `logs/` ใน `.gitignore` (มีอยู่แล้วใน pattern `[Ll]ogs/`)

### Implementation
- [x] สร้าง `Logging/LogEntry.cs`
- [x] สร้าง `Logging/ILogChannel.cs`
- [x] สร้าง `Logging/LogChannel.cs` (BoundedChannel, capacity 2048)
- [x] สร้าง `Logging/LogBackgroundService.cs`
- [x] สร้าง `Logging/LogSummaryService.cs` (PeriodicTimer 5 นาที)
- [x] สร้าง `Middleware/LoggingMiddleware.cs`

### Program.cs
- [x] เพิ่ม Serilog configuration (JSON formatter, rolling daily, retain 30 วัน)
- [x] Register `ILogChannel` เป็น **Singleton**
- [x] Register `LogSummaryService` เป็น **Singleton**
- [x] Register `LogBackgroundService` เป็น **HostedService**
- [x] Register `LogSummaryService` เป็น **HostedService** (ใช้ instance เดิมจาก DI)
- [x] เรียก `app.UseMiddleware<LoggingMiddleware>()` ก่อน `UseAuthentication`

### Verification
- [x] Build ผ่านไม่มี error
- [ ] รัน API แล้วมีไฟล์ `logs/audit-YYYYMMDD.json` สร้างขึ้น
- [ ] Log entry มี field: `Timestamp`, `Method`, `Path`, `StatusCode`, `UserName`, `IpAddress`, `ElapsedMs`, `TraceId`
- [ ] Log entry ของ Admin endpoints มี `UserName` เป็น Basic Auth username (ถ้า extract ได้)
- [ ] หลังจากรอ 5 นาที มีไฟล์ `logs/summary-YYYYMMDD.json` สร้างขึ้น
- [ ] ทดสอบ Graceful Shutdown — app ปิดแล้ว log ที่ค้างใน Channel ถูก flush
- [ ] ตรวจสอบว่า `ElapsedMs` ของ endpoint ปกติไม่เพิ่มขึ้นมากกว่า 1 ms

---

## ข้อควรระวัง

| ประเด็น | แนวทาง |
|---|---|
| `LogBackgroundService` ต้องการ Serilog `ILogger` ไม่ใช่ `ILogger<T>` | Inject `Serilog.ILogger` โดยตรง หรือใช้ `Log.Logger` |
| `LogSummaryService` ต้อง register ทั้ง Singleton และ HostedService | ใช้ `AddHostedService(sp => sp.GetRequiredService<LogSummaryService>())` ไม่ใช่ `AddHostedService<LogSummaryService>()` (จะสร้าง instance ใหม่) |
| ลำดับ Middleware | `LoggingMiddleware` ต้องอยู่ก่อน `UseAuthentication` เพื่อ log ทุก request รวมถึง 401 |
| ไม่ log request body | หากต้องการ log body ต้องทำ `EnableBuffering()` ก่อน read เพื่อไม่ให้ stream ถูก consume |
| Sensitive data | **อย่า log** password, token, หรือ phone number (field นี้ encrypt อยู่แล้วใน DB) |
