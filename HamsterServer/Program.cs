namespace HamsterServer;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new System.Threading.Mutex(true, "HamsterServer_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("Hamster Server is already running.\nCheck the system tray.",
                "Hamster Server", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            LogCrash("UI thread", e.Exception);
            MessageBox.Show(e.Exception.Message, "Unhandled Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        // Background-thread / finalizer exceptions are NOT caught by ThreadException.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogCrash("Background thread", e.ExceptionObject as Exception);

        // Faults inside fire-and-forget tasks (watchdog restarts, log flushes) never surface
        // anywhere else — record them instead of letting the finalizer swallow them silently.
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogCrash("Unobserved task", e.Exception);
            e.SetObserved();
        };

        Application.Run(new HamsterServerForm());
    }

    private static void LogCrash(string source, Exception? ex)
    {
        if (ex is null) return;
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "config", "logs");
            Directory.CreateDirectory(dir);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(dir, "crash.log"), line);
        }
        catch { /* never let crash-logging itself throw */ }
    }
}
