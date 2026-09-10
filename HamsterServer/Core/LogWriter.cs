namespace HamsterServer.Core;

public sealed class LogWriter : IDisposable
{
    private readonly string _logDir;
    private readonly object _lock = new();
    private string _currentFilePath = "";
    private DateTime _currentDate = DateTime.MinValue;

    public LogWriter(string rootDir)
    {
        _logDir = Path.Combine(rootDir, "config", "logs", "server");
        Directory.CreateDirectory(_logDir);
    }

    public void Write(string text)
    {
        lock (_lock)
        {
            EnsureFile(DateTime.Today);
            try { File.AppendAllText(_currentFilePath, text + Environment.NewLine); }
            catch { /* swallow — log write failure must not crash the UI */ }
        }
    }

    // Append many lines in a single file open — avoids one open/close per line
    // when the UI flushes a batch of queued log entries.
    public void WriteBatch(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0) return;
        lock (_lock)
        {
            EnsureFile(DateTime.Today);
            try
            {
                var sb = new System.Text.StringBuilder(lines.Count * 64);
                foreach (var line in lines)
                    sb.Append(line).Append(Environment.NewLine);
                File.AppendAllText(_currentFilePath, sb.ToString());
            }
            catch { /* swallow — log write failure must not crash the UI */ }
        }
    }

    private void EnsureFile(DateTime today)
    {
        if (today == _currentDate) return;
        _currentDate = today;
        _currentFilePath = Path.Combine(_logDir, $"hamster-server-{today:yyyy-MM-dd}.log");
        PruneOldLogs(today);  // Fix 2.1: xóa log cũ hơn 30 ngày mỗi khi sang ngày mới
    }

    // Xóa server log files cũ hơn 30 ngày để tránh disk fill.
    // Chỉ xóa hamster-server-*.log (không động vào Apache/PHP access/error logs).
    private void PruneOldLogs(DateTime today)
    {
        try
        {
            var cutoff = today.AddDays(-30);
            foreach (var f in Directory.GetFiles(_logDir, "hamster-server-*.log"))
            {
                if (File.GetLastWriteTime(f) < cutoff)
                    File.Delete(f);
            }
        }
        catch { /* prune failure must not crash the app */ }
    }

    public void Dispose() { }
}
