using System.Diagnostics;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

public sealed class ProcessOptimizer : IProcessOptimizer
{
    private static readonly HashSet<string> ProtectedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "Registry", "smss", "csrss", "wininit", "winlogon", "services", "lsass", "svchost",
        "dwm", "explorer", "fontdrvhost", "sihost", "taskhostw", "ctfmon", "RuntimeBroker", "SearchHost",
        "StartMenuExperienceHost", "ShellExperienceHost", "TextInputHost", "MsMpEng", "SecurityHealthService"
    };

    public IReadOnlyList<RunningProcessInfo> GetTopProcesses(int count)
    {
        var currentId = Environment.ProcessId;
        var sessionId = Process.GetCurrentProcess().SessionId;
        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var result = new List<RunningProcessInfo>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.Id == currentId || process.SessionId != sessionId || IsProtected(process, windowsDir))
                    {
                        continue;
                    }

                    result.Add(new RunningProcessInfo(process.Id, process.ProcessName, process.WorkingSet64, process.MainWindowHandle != IntPtr.Zero));
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                {
                    // Processus terminé ou inaccessible : ignoré.
                }
            }
        }

        return result.OrderByDescending(p => p.MemoryBytes).Take(count).ToList();
    }

    public bool TryClose(int processId, out string? error)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (processId == Environment.ProcessId || IsProtected(process, Environment.GetFolderPath(Environment.SpecialFolder.Windows)))
            {
                error = "Ce processus est protégé.";
                return false;
            }

            if (process.MainWindowHandle != IntPtr.Zero && process.CloseMainWindow() && process.WaitForExit(3000))
            {
                error = null;
                return true;
            }

            process.Kill();
            error = null;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool IsProtected(Process process, string windowsDir)
    {
        if (ProtectedNames.Contains(process.ProcessName))
        {
            return true;
        }

        try
        {
            var path = process.MainModule?.FileName;
            return path is null || path.StartsWith(windowsDir, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return true;
        }
    }
}
