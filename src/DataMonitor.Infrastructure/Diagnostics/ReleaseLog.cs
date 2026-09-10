namespace DataMonitor.Infrastructure.Diagnostics;

public static class ReleaseLog
{
    private static readonly object Gate = new();
    public static void Write(string operation, Exception? exception = null)
    {
        try
        {
            lock (Gate)
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DataMonitor", "Logs");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, "DataMonitor.log");
                if (File.Exists(path) && new FileInfo(path).Length > 2 * 1024 * 1024)
                    File.Move(path, path + ".previous", true);
                File.AppendAllText(path, $"{DateTimeOffset.Now:O} {operation}{Environment.NewLine}{exception}{Environment.NewLine}");
            }
        }
        catch { /* A logging failure must not hide the original error. */ }
    }
}
