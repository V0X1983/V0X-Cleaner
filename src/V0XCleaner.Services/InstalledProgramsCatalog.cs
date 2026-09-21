using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Cleaners;
using V0XCleaner.Services.Native;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services;

/// <summary>
/// Catalogue des programmes installés : registre Uninstall (HKCU/HKLM/WOW6432Node) pour les
/// applications classiques, et Get-AppxPackage/Remove-AppxPackage (via PowerShell) pour les
/// applications du Microsoft Store — aucune API Win32 simple n'existe pour piloter les paquets
/// AppX depuis une application non empaquetée.
/// </summary>
public sealed class InstalledProgramsCatalog(
    IRegistryBackupService backupService,
    IPathGuard pathGuard,
    ILoggerFactory loggerFactory) : IInstalledProgramsCatalog
{
    private static readonly (RegistryKey Hive, string HivePrefix, string SubPath)[] UninstallRoots =
    [
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
        (Registry.LocalMachine, "HKLM", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
        (Registry.CurrentUser, "HKCU", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall")
    ];

    private readonly ILogger<InstalledProgramsCatalog> _logger = loggerFactory.CreateLogger<InstalledProgramsCatalog>();

    public async Task<IReadOnlyList<InstalledProgram>> GetProgramsAsync(CancellationToken cancellationToken = default)
    {
        var programs = new List<InstalledProgram>();
        programs.AddRange(GetWin32Programs(cancellationToken));
        programs.AddRange(await GetUwpProgramsAsync(cancellationToken));
        return programs;
    }

    public async Task<UninstallOutcome> UninstallAsync(InstalledProgram program, bool preferSilent, CancellationToken cancellationToken = default)
    {
        if (program.Kind == InstalledProgramKind.UwpPackage)
        {
            var script = $"Remove-AppxPackage -Package '{EscapeSingleQuotes(program.Id)}'";
            var (success, _, stdErr, _) = await PowerShellRunner.RunAsync(script, cancellationToken);
            return success
                ? new UninstallOutcome(true, "Application supprimée.")
                : new UninstallOutcome(false, string.IsNullOrWhiteSpace(stdErr) ? "Échec de la suppression." : stdErr.Trim());
        }

        var command = preferSilent && !string.IsNullOrWhiteSpace(program.QuietUninstallCommand)
            ? program.QuietUninstallCommand
            : program.UninstallCommand;

        if (string.IsNullOrWhiteSpace(command))
        {
            return new UninstallOutcome(false, "Aucune commande de désinstallation enregistrée pour ce programme.");
        }

        if (preferSilent
            && string.IsNullOrWhiteSpace(program.QuietUninstallCommand)
            && command.Contains("msiexec", StringComparison.OrdinalIgnoreCase)
            && !command.Contains("/q", StringComparison.OrdinalIgnoreCase))
        {
            command += " /qn /norestart";
        }

        return await RunUninstallCommandAsync(command, cancellationToken);
    }

    public async Task<UninstallOutcome> ForceRemoveResidueAsync(InstalledProgram program, CancellationToken cancellationToken = default)
    {
        if (program.Kind != InstalledProgramKind.Win32)
        {
            return new UninstallOutcome(false, "La suppression forcée ne s'applique qu'aux programmes classiques.");
        }

        var messages = new List<string>();
        var overallSuccess = true;

        if (!string.IsNullOrWhiteSpace(program.InstallLocation) && Directory.Exists(program.InstallLocation))
        {
            if (pathGuard.IsSafeToDelete(program.InstallLocation, out var reason))
            {
                try
                {
                    Directory.Delete(program.InstallLocation, recursive: true);
                    messages.Add("Dossier d'installation supprimé.");
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                {
                    overallSuccess = false;
                    messages.Add($"Échec de la suppression du dossier : {ex.Message}");
                }
            }
            else
            {
                overallSuccess = false;
                messages.Add($"Dossier d'installation non supprimé ({reason}).");
            }
        }

        var keyCleaner = new RegistryKeyDeletionCleaner(backupService, loggerFactory.CreateLogger<RegistryKeyDeletionCleaner>());
        var result = await keyCleaner.CleanAsync(
            [new CleanupItem { Id = program.Id, DisplayPath = program.Id, Category = CleanupCategory.Registry, SizeBytes = 0 }],
            OperationMode.Execute,
            cancellationToken);

        if (result.FailedCount == 0)
        {
            messages.Add("Entrée de registre supprimée (sauvegarde .reg créée automatiquement).");
        }
        else
        {
            overallSuccess = false;
            messages.Add($"Échec de la suppression de l'entrée de registre : {result.Errors.FirstOrDefault()?.Message}");
        }

        return new UninstallOutcome(overallSuccess, string.Join(' ', messages));
    }

    public async Task<UninstallOutcome> MoveAsync(InstalledProgram program, string destinationParentFolder, CancellationToken cancellationToken = default)
    {
        if (program.Kind != InstalledProgramKind.Win32 || string.IsNullOrWhiteSpace(program.InstallLocation))
        {
            return new UninstallOutcome(false, "Le déplacement ne s'applique qu'aux programmes classiques dont le dossier d'installation est connu.");
        }

        string source;
        string destination;
        try
        {
            source = Path.GetFullPath(program.InstallLocation.Trim().Trim('"')).TrimEnd('\\');
            destination = Path.Combine(Path.GetFullPath(destinationParentFolder), Path.GetFileName(source));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new UninstallOutcome(false, $"Chemin invalide : {ex.Message}");
        }

        if (!Directory.Exists(source))
        {
            return new UninstallOutcome(false, "Le dossier d'installation n'existe plus.");
        }

        if (new DirectoryInfo(source).Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            return new UninstallOutcome(false, "Ce programme a déjà été déplacé (le dossier est une jonction).");
        }

        if (!pathGuard.IsSafeToDelete(source, out var reason))
        {
            return new UninstallOutcome(false, $"Dossier protégé, déplacement refusé ({reason}).");
        }

        if (destination.StartsWith(source + "\\", StringComparison.OrdinalIgnoreCase) || string.Equals(destination, source, StringComparison.OrdinalIgnoreCase))
        {
            return new UninstallOutcome(false, "La destination ne peut pas se trouver dans le dossier source.");
        }

        if (Directory.Exists(destination) || File.Exists(destination))
        {
            return new UninstallOutcome(false, $"\"{destination}\" existe déjà.");
        }

        var (hivePrefix, subKeyPath) = RegistryPathHelper.SplitKeyId(program.Id);
        var backupPath = await backupService.BackupKeysAsync([program.Id], cancellationToken);
        if (backupPath is null)
        {
            return new UninstallOutcome(false, "Sauvegarde du registre impossible : déplacement annulé par précaution.");
        }

        try
        {
            Directory.CreateDirectory(destinationParentFolder);
            await Task.Run(() => MoveDirectory(source, destination, cancellationToken), cancellationToken);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Échec du déplacement de {Source} vers {Destination}", source, destination);
            return new UninstallOutcome(false, $"Échec du déplacement : {ex.Message}");
        }

        // Jonction à l'ancien emplacement : les raccourcis, le désinstalleur et les autres références restent valides.
        var (linked, _, linkErr, _) = await PowerShellRunner.RunAsync(
            $"New-Item -ItemType Junction -Path '{EscapeSingleQuotes(source)}' -Target '{EscapeSingleQuotes(destination)}' -ErrorAction Stop | Out-Null",
            cancellationToken);
        if (!linked)
        {
            try
            {
                MoveDirectory(destination, source, CancellationToken.None);
                return new UninstallOutcome(false, $"Jonction impossible, le dossier a été remis en place. {linkErr.Trim()}");
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                return new UninstallOutcome(false, $"Jonction impossible et retour arrière échoué : le programme se trouve maintenant dans \"{destination}\". {ex.Message}");
            }
        }

        try
        {
            using var key = RegistryPathHelper.GetBaseKey(hivePrefix).OpenSubKey(subKeyPath, writable: true);
            key?.SetValue("InstallLocation", destination, RegistryValueKind.String);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return new UninstallOutcome(true, $"Programme déplacé vers \"{destination}\" (jonction créée), mais l'entrée de registre n'a pas pu être mise à jour : {ex.Message}");
        }

        return new UninstallOutcome(true, $"Programme déplacé vers \"{destination}\". Une jonction a été laissée à l'ancien emplacement.");
    }

    /// <summary>Déplace un dossier : renommage direct sur le même volume, sinon copie puis suppression de la source.</summary>
    private static void MoveDirectory(string source, string destination, CancellationToken cancellationToken)
    {
        if (string.Equals(Path.GetPathRoot(source), Path.GetPathRoot(destination), StringComparison.OrdinalIgnoreCase))
        {
            Directory.Move(source, destination);
            return;
        }

        try
        {
            CopyDirectory(source, destination, cancellationToken);
        }
        catch
        {
            try { Directory.Delete(destination, recursive: true); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            throw;
        }

        Directory.Delete(source, recursive: true);
    }

    private static void CopyDirectory(string source, string destination, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (var dir in Directory.EnumerateDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)), cancellationToken);
        }
    }

    private IReadOnlyList<InstalledProgram> GetWin32Programs(CancellationToken cancellationToken)
    {
        var programs = new List<InstalledProgram>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (hive, hivePrefix, subPath) in UninstallRoots)
        {
            using var baseKey = RegistrySafe.OpenSubKey(hive, subPath);
            if (baseKey is null)
            {
                continue;
            }

            foreach (var subName in RegistrySafe.GetSubKeyNames(baseKey))
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var subKey = RegistrySafe.OpenSubKey(baseKey, subName);
                if (subKey is null)
                {
                    continue;
                }

                if (RegistrySafe.GetValue(subKey, "SystemComponent") is int systemComponent && systemComponent == 1)
                {
                    continue;
                }

                if (RegistrySafe.GetValue(subKey, "ParentKeyName") is string parent && !string.IsNullOrWhiteSpace(parent))
                {
                    continue;
                }

                if (RegistrySafe.GetValue(subKey, "DisplayName") is not string displayName || string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                var id = $@"{hivePrefix}\{subPath}\{subName}";
                if (!seen.Add(id))
                {
                    continue;
                }

                long? sizeBytes = RegistrySafe.GetValue(subKey, "EstimatedSize") is int sizeKb ? (long)sizeKb * 1024 : null;

                programs.Add(new InstalledProgram
                {
                    Id = id,
                    DisplayName = displayName,
                    DisplayVersion = RegistrySafe.GetValue(subKey, "DisplayVersion") as string,
                    Publisher = RegistrySafe.GetValue(subKey, "Publisher") as string,
                    EstimatedSizeBytes = sizeBytes,
                    InstallDate = TryParseInstallDate(RegistrySafe.GetValue(subKey, "InstallDate") as string),
                    Kind = InstalledProgramKind.Win32,
                    UninstallCommand = RegistrySafe.GetValue(subKey, "UninstallString") as string,
                    QuietUninstallCommand = RegistrySafe.GetValue(subKey, "QuietUninstallString") as string,
                    InstallLocation = RegistrySafe.GetValue(subKey, "InstallLocation") as string,
                    IconPath = ResolveIconPath(RegistrySafe.GetValue(subKey, "DisplayIcon") as string,
                        RegistrySafe.GetValue(subKey, "UninstallString") as string),
                    CanUninstall = true
                });
            }
        }

        return programs;
    }

    /// <summary>Extrait un chemin de fichier exploitable depuis DisplayIcon ("C:\\x.exe,0", entre guillemets...) ou, à défaut, UninstallString.</summary>
    internal static string? ResolveIconPath(string? displayIcon, string? uninstallString)
    {
        foreach (var raw in new[] { displayIcon, uninstallString })
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var text = raw.Trim();
            string path;
            if (text[0] == '"')
            {
                var end = text.IndexOf('"', 1);
                path = end > 1 ? text[1..end] : text.Trim('"');
            }
            else
            {
                var exeIndex = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                var icoIndex = text.IndexOf(".ico", StringComparison.OrdinalIgnoreCase);
                var cut = exeIndex >= 0 ? exeIndex + 4 : icoIndex >= 0 ? icoIndex + 4 : -1;
                path = cut > 0 ? text[..cut] : text.Split(',')[0];
            }

            path = Environment.ExpandEnvironmentVariables(path.Trim());
            if (path.Length > 0 && File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<InstalledProgram>> GetUwpProgramsAsync(CancellationToken cancellationToken)
    {
        // -InputObject (plutôt qu'un pipe vers ConvertTo-Json) est essentiel : PowerShell énumère
        // toujours un pipeline élément par élément, donc un tableau à 0 ou 1 résultat piperait un
        // objet JSON isolé au lieu d'un tableau. -InputObject préserve le tableau tel quel.
        const string script = """
            $packages = @(Get-AppxPackage | Where-Object { -not $_.IsFramework -and -not $_.IsResourcePackage } |
                Select-Object Name, PackageFullName, Publisher, InstallLocation, NonRemovable, @{Name='VersionString';Expression={$_.Version.ToString()}})
            ConvertTo-Json -Compress -InputObject $packages
            """;

        var (success, stdOut, stdErr, _) = await PowerShellRunner.RunAsync(script, cancellationToken);
        if (!success)
        {
            _logger.LogWarning("Échec de l'énumération des applications du Microsoft Store : {Error}", stdErr);
            return [];
        }

        if (string.IsNullOrWhiteSpace(stdOut))
        {
            return [];
        }

        try
        {
            var dtos = JsonSerializer.Deserialize<List<AppxPackageDto>>(stdOut, JsonOptions) ?? [];
            return dtos.Select(ToInstalledProgram).ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Réponse PowerShell inattendue lors de l'énumération des applications du Microsoft Store.");
            return [];
        }
    }

    private static InstalledProgram ToInstalledProgram(AppxPackageDto dto) => new()
    {
        Id = dto.PackageFullName ?? dto.Name ?? Guid.NewGuid().ToString(),
        DisplayName = dto.Name ?? dto.PackageFullName ?? "Application inconnue",
        DisplayVersion = dto.VersionString,
        Publisher = dto.Publisher,
        EstimatedSizeBytes = null,
        InstallDate = null,
        Kind = InstalledProgramKind.UwpPackage,
        UninstallCommand = null,
        QuietUninstallCommand = null,
        InstallLocation = dto.InstallLocation,
        CanUninstall = !dto.NonRemovable
    };

    private static async Task<UninstallOutcome> RunUninstallCommandAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo("cmd.exe", $"/c \"{command}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new UninstallOutcome(false, "Impossible de démarrer le désinstalleur.");
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0
                ? new UninstallOutcome(true, "Désinstallation terminée.")
                : new UninstallOutcome(false, $"Le désinstalleur a retourné le code {process.ExitCode}.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new UninstallOutcome(false, ex.Message);
        }
    }

    private static DateTime? TryParseInstallDate(string? raw) =>
        !string.IsNullOrWhiteSpace(raw)
        && DateTime.TryParseExact(raw, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static string EscapeSingleQuotes(string value) => value.Replace("'", "''");

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private sealed class AppxPackageDto
    {
        public string? Name { get; set; }
        public string? PackageFullName { get; set; }
        public string? Publisher { get; set; }
        public string? InstallLocation { get; set; }
        public bool NonRemovable { get; set; }

        [JsonPropertyName("VersionString")]
        public string? VersionString { get; set; }
    }
}
