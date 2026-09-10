namespace DataMonitor.Database;

public static class DatabasePath
{
    public static string GetDatabasePath()
    {
        var localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        var directory =
            Path.Combine(
                localAppData,
                "DataMonitor");

        Directory.CreateDirectory(directory);

        return Path.Combine(
            directory,
            "DataMonitor.db");
    }
}