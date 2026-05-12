using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Logging;

public class LogChannel : ILogChannel
{
    private readonly Channel<LogEntry> _channel;

    // สร้าง buffer บน RAM เก็บ log ได้ 2048 entries ถ้าเต็มให้ทิ้งของเก่าสุด
    public LogChannel()
    {
        _channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(2048)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
    }

    // เขียน LogEntry ลง Channel buffer แบบ async และประหยัด memory
    
    public ValueTask WriteAsync(LogEntry entry, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(entry, ct);

    // 
    public async IAsyncEnumerable<LogEntry> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // อ่าน LogEntry จาก Channel ทีละตัวแบบ async
        // ct = ตัวหยุดส่ง
        await foreach (LogEntry entry in _channel.Reader.ReadAllAsync(ct))
        {
            // แล้วส่งออกไปทีละตัว (yield)
            yield return entry;
        }
    }
}
