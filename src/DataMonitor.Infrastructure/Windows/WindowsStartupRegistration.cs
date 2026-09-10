using System.Runtime.Versioning;
using DataMonitor.Core.Interfaces;
using Microsoft.Win32;

namespace DataMonitor.Infrastructure.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsStartupRegistration(string executablePath) : IStartupRegistration
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DataMonitor";
    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(ValueName) is string;
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, true);
        if (enabled)
        {
            if (!Path.IsPathFullyQualified(executablePath) || !File.Exists(executablePath) || executablePath.Contains('"'))
                throw new InvalidOperationException("The application executable is unavailable for startup registration.");
            key.SetValue(ValueName, $"\"{executablePath}\"", RegistryValueKind.String);
        }
        else key.DeleteValue(ValueName, false);
    }
}
