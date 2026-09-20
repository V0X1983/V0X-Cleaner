using System.Security;
using Microsoft.Win32;

namespace V0XCleaner.Services.RegistryCleanup;

/// <summary>Accès registre tolérants aux clés inaccessibles (permissions) : on ignore plutôt que planter.</summary>
internal static class RegistrySafe
{
    public static IReadOnlyList<string> GetSubKeyNames(RegistryKey key)
    {
        try
        {
            return key.GetSubKeyNames();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or ObjectDisposedException)
        {
            return [];
        }
    }

    public static IReadOnlyList<string> GetValueNames(RegistryKey key)
    {
        try
        {
            return key.GetValueNames();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or ObjectDisposedException)
        {
            return [];
        }
    }

    public static RegistryKey? OpenSubKey(RegistryKey baseKey, string name, bool writable = false)
    {
        try
        {
            return baseKey.OpenSubKey(name, writable);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or ArgumentException or ObjectDisposedException)
        {
            return null;
        }
    }

    public static object? GetValue(RegistryKey key, string? valueName)
    {
        try
        {
            return key.GetValue(valueName);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            return null;
        }
    }
}
