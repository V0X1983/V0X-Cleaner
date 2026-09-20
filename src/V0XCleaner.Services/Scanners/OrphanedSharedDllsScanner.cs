using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services.Scanners;

/// <summary>
/// SharedDlls : chaque nom de valeur est un chemin complet de fichier partagé (compteur de
/// références en donnée). Signalé si le fichier référencé n'existe plus.
/// </summary>
public sealed class OrphanedSharedDllsScanner : IScanner
{
    private const string KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDlls";

    public string Key => "registry-shared-dlls";

    public CleanupCategory Category => CleanupCategory.Registry;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanupItem>();

        using var key = RegistrySafe.OpenSubKey(Registry.LocalMachine, KeyPath);
        if (key is not null)
        {
            foreach (var valueName in RegistrySafe.GetValueNames(key))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(valueName))
                {
                    continue;
                }

                var expanded = Environment.ExpandEnvironmentVariables(valueName);
                if (File.Exists(expanded))
                {
                    continue;
                }

                items.Add(new CleanupItem
                {
                    Id = $@"HKLM\{KeyPath}||{valueName}",
                    DisplayPath = $@"HKEY_LOCAL_MACHINE\{KeyPath}",
                    Category = Category,
                    SizeBytes = 0,
                    Description = $"Référence à une DLL partagée introuvable : {expanded}"
                });
            }
        }

        return Task.FromResult(new ScanResult { Items = items });
    }
}
