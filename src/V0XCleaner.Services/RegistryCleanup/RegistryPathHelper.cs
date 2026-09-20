using Microsoft.Win32;

namespace V0XCleaner.Services.RegistryCleanup;

/// <summary>
/// Convertit un préfixe de ruche court ("HKCU", "HKLM", "HKCR", "HKU") vers la RegistryKey de base
/// correspondante. Convention utilisée dans tout le module Registre pour identifier un chemin
/// de façon compacte dans CleanupItem.Id (ex: "HKLM\Software\...\Uninstall\{GUID}").
/// </summary>
internal static class RegistryPathHelper
{
    public static RegistryKey GetBaseKey(string hivePrefix) => hivePrefix switch
    {
        "HKCU" => Registry.CurrentUser,
        "HKLM" => Registry.LocalMachine,
        "HKCR" => Registry.ClassesRoot,
        "HKU" => Registry.Users,
        _ => throw new ArgumentOutOfRangeException(nameof(hivePrefix), hivePrefix, "Ruche de registre non prise en charge.")
    };

    public static string GetHivePrefix(RegistryKey hive)
    {
        if (ReferenceEquals(hive, Registry.CurrentUser)) return "HKCU";
        if (ReferenceEquals(hive, Registry.LocalMachine)) return "HKLM";
        if (ReferenceEquals(hive, Registry.ClassesRoot)) return "HKCR";
        if (ReferenceEquals(hive, Registry.Users)) return "HKU";
        throw new ArgumentOutOfRangeException(nameof(hive), hive, "Ruche de registre non prise en charge.");
    }

    /// <summary>Sépare un identifiant "HKLM\Chemin\Vers\Clé" en préfixe de ruche + sous-chemin.</summary>
    public static (string HivePrefix, string SubKeyPath) SplitKeyId(string id)
    {
        var idx = id.IndexOf('\\');
        return idx < 0 ? (id, string.Empty) : (id[..idx], id[(idx + 1)..]);
    }

    /// <summary>Sépare le chemin parent et le nom de la dernière sous-clé d'un sous-chemin.</summary>
    public static (string ParentPath, string LeafName) SplitParent(string subKeyPath)
    {
        var idx = subKeyPath.LastIndexOf('\\');
        return idx < 0 ? (string.Empty, subKeyPath) : (subKeyPath[..idx], subKeyPath[(idx + 1)..]);
    }
}
