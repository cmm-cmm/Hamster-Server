using System.Threading.Channels;

namespace HamsterServer.Core;

public sealed class LogWriter : IDisposable
{
    private readonly string _logDir;
    private readonly Channel<string> _channel;
    private readonly Task _writerTask;
    private string _currentFilePath = "";
    private DateTime _currentDate = DateTime.MinValue;
    private int _completed;

    public LogWriter(string rootDir)
    {
        _logDir = Path.Combine(rootDir, "config", "logs", "server");
        Directory.CreateDirectory(_logDir);
        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _writerTask = Task.Run(ProcessQueueAsync);
    }

    public bool Enqueue(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Volatile.Read(ref _completed) == 0 && _channel.Writer.TryWrite(text);
    }

    private async Task ProcessQueueAsync()
    {
        var batch = new List<string>(128);
        try
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var line)) batch.Add(line);
                WriteBatch(batch);
                batch.Clear();
            }
        }
        catch { /* logging must never crash the process */ }
    }

    // A single consumer preserves ordering and batches bursts into one file open.
    private void WriteBatch(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0) return;
        try
        {
            EnsureFile(DateTime.Today);
            var sb = new System.Text.StringBuilder(lines.Count * 64);
            foreach (var line in lines)
                sb.Append(line).Append(Environment.NewLine);
            File.AppendAllText(_currentFilePath, sb.ToString());
        }
        catch { /* log write failure must not crash the UI */ }
    }

    private void EnsureFile(DateTime today)
    {
        if (today == _currentDate) return;
        _currentDate = today;
        _currentFilePath = Path.Combine(_logDir, $"hamster-server-{today:yyyy-MM-dd}.log");
        PruneOldLogs(today);  // Fix 2.1: xóa log cũ hơn 30 ngày mỗi khi sang ngày mới
    }

    // Chỉ dọn log do app này sinh ra — không động vào Apache/PHP access/error logs.
    // xserver-*.log là tên trước khi đổi tên sang Hamster Server: không còn được ghi nữa
    // nhưng vẫn phải nằm trong retention, nếu không nó tồn tại vĩnh viễn trên máy user.
    private static readonly string[] ServerLogPatterns = ["hamster-server-*.log", "xserver-*.log"];

    // Xóa server log files cũ hơn 30 ngày để tránh disk fill.
    private void PruneOldLogs(DateTime today)
    {
        try
        {
            var cutoff = today.AddDays(-30);
            foreach (var pattern in ServerLogPatterns)
            {
                foreach (var f in Directory.GetFiles(_logDir, pattern))
                {
                    if (File.GetLastWriteTime(f) < cutoff)
                        File.Delete(f);
                }
            }
        }
        catch { /* prune failure must not crash the app */ }
    }

    public bool CompleteAndFlush(TimeSpan timeout)
    {
        if (Interlocked.Exchange(ref _completed, 1) == 0)
            _channel.Writer.TryComplete();

        try { return _writerTask.Wait(timeout); }
        catch { return false; }
    }

    public void Dispose() => CompleteAndFlush(TimeSpan.FromSeconds(2));
}
