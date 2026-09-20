using Microsoft.Win32;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Startup;

/// <summary>
/// Lit/écrit le même indicateur binaire "StartupApproved" que le Gestionnaire des tâches Windows
/// utilise pour activer/désactiver un élément de démarrage SANS supprimer la valeur Run ou le
/// raccourci d'origine. Format (non documenté officiellement mais stable depuis Windows 8) :
/// une valeur REG_BINARY de 12 octets dont le premier détermine l'état (0x02 = actif, 0x03 = inactif).
/// </summary>
internal static class StartupApprovedHelper
{
    public static bool IsEnabled(RegistryKey hive, string approvedSubKeyPath, string valueName)
    {
        using var key = RegistrySafe.OpenSubKey(hive, approvedSubKeyPath);
        if (key is null)
        {
            return true; // Jamais désactivé par l'utilisateur : actif par défaut.
        }

        var raw = RegistrySafe.GetValue(key, valueName) as byte[];
        if (raw is null || raw.Length == 0)
        {
            return true;
        }

        return raw[0] == 0x02;
    }

    public static void SetEnabled(RegistryKey hive, string approvedSubKeyPath, string valueName, bool enabled)
    {
        using var key = hive.CreateSubKey(approvedSubKeyPath, writable: true);
        var data = new byte[12];
        data[0] = enabled ? (byte)0x02 : (byte)0x03;
        key.SetValue(valueName, data, RegistryValueKind.Binary);
    }
}
