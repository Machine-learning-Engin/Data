namespace DataMonitor.MonitorService;

// An OS-held file handle is released even after an abnormal process exit.
public sealed class MonitoringLease(string path) : IDisposable
{
    private FileStream? _handle;
    public bool TryAcquire()
    {
        if (_handle is not null) return true;
        try { _handle = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); return true; }
        catch (IOException) { return false; }
    }
    public void Dispose() { _handle?.Dispose(); _handle = null; }
}
