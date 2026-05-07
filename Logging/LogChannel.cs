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
