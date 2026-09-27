using System.Runtime.InteropServices;
using System.Text;

namespace V0XCleaner.Services.Native;

/// <summary>
/// Détecte si le process courant tourne avec une identité de paquet MSIX (Phase 6/7), via
/// <c>GetCurrentPackageFamilyName</c> (kernel32) plutôt que les API WinRT <c>Windows.ApplicationModel.Package</c> :
/// évite d'ajouter une dépendance WinRT à <c>V0XCleaner.Services</c> (net10.0-windows, sans version
/// de plateforme cible) pour une simple vérification, et fonctionne identiquement pour l'app WPF
/// (jamais empaquetée : <see cref="TryGetFamilyName"/> retourne toujours <see langword="false"/>) et
/// l'app WinUI packagée.
/// </summary>
public static class PackageIdentity
{
    private const int ErrorInsufficientBuffer = 122;
    private const int ApiErrorNoPackage = 15700;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFamilyName(ref int packageFamilyNameLength, StringBuilder? packageFamilyName);

    /// <summary>Retourne le PackageFamilyName (ex: "5EE5DEF2-....V0X_abcdefg0h1j2k") si le process est empaqueté.</summary>
    public static bool TryGetFamilyName(out string? familyName)
    {
        var length = 0;
        var rc = GetCurrentPackageFamilyName(ref length, null);
        if (rc == ApiErrorNoPackage)
        {
            familyName = null;
            return false;
        }

        if (rc != ErrorInsufficientBuffer)
        {
            familyName = null;
            return false;
        }

        var buffer = new StringBuilder(length);
        rc = GetCurrentPackageFamilyName(ref length, buffer);
        if (rc != 0)
        {
            familyName = null;
            return false;
        }

        familyName = buffer.ToString();
        return true;
    }
}
