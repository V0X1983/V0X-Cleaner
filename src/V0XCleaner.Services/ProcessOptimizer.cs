using System.Collections.Concurrent;
using System.Diagnostics;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services;

public sealed class ProcessOptimizer : IProcessOptimizer
{
    private static readonly HashSet<string> ProtectedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "Registry", "smss", "csrss", "wininit", "winlogon", "services", "lsass", "svchost",
        "dwm", "explorer", "fontdrvhost", "sihost", "taskhostw", "ctfmon", "RuntimeBroker", "SearchHost",
        "StartMenuExperienceHost", "ShellExperienceHost", "TextInputHost", "MsMpEng", "SecurityHealthService"
    };

    /// <summary>Processus mis en veille par nous, avec leur heure de démarrage pour ne pas confondre un PID réutilisé.</summary>
    private readonly ConcurrentDictionary<int, DateTime> _sleeping = new();

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

    public bool TrySuspend(int processId, out string? error)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            if (processId == Environment.ProcessId || IsProtected(process, Environment.GetFolderPath(Environment.SpecialFolder.Windows)))
            {
                error = "Ce processus est protégé.";
                return false;
            }

            var startTime = process.StartTime;
            if (!ApplyToProcess(processId, NtDllInterop.NtSuspendProcess, out error))
            {
                return false;
            }

            _sleeping[processId] = startTime;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            error = ex.Message;
            return false;
        }
    }

    public bool TryResume(int processId, out string? error)
    {
        var ok = ApplyToProcess(processId, NtDllInterop.NtResumeProcess, out error);
        if (ok || error is not null)
        {
            // Si le processus n'existe plus, on l'oublie aussi.
            _sleeping.TryRemove(processId, out _);
        }

        return ok;
    }

    public IReadOnlyList<RunningProcessInfo> GetSleepingProcesses()
    {
        var result = new List<RunningProcessInfo>();
        foreach (var (processId, startTime) in _sleeping.ToArray())
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                if (process.StartTime != startTime)
                {
                    _sleeping.TryRemove(processId, out _);
                    continue;
                }

                result.Add(new RunningProcessInfo(processId, process.ProcessName, process.WorkingSet64, process.MainWindowHandle != IntPtr.Zero));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                _sleeping.TryRemove(processId, out _);
            }
        }

        return result.OrderByDescending(p => p.MemoryBytes).ToList();
    }

    public void ResumeAll()
    {
        foreach (var processId in _sleeping.Keys.ToArray())
        {
            TryResume(processId, out _);
        }
    }

    private static bool ApplyToProcess(int processId, Func<nint, int> action, out string? error)
    {
        var handle = NtDllInterop.OpenProcess(NtDllInterop.ProcessSuspendResume, false, processId);
        if (handle == 0)
        {
            error = "Accès refusé ou processus introuvable (essayez en administrateur).";
            return false;
        }

        try
        {
            var status = action(handle);
            error = status == 0 ? null : $"Échec (code {status:X}).";
            return status == 0;
        }
        finally
        {
            NtDllInterop.CloseHandle(handle);
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
