using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.Services;

public sealed partial class DriverCatalog(ILogger<DriverCatalog> logger) : IDriverCatalog
{
    private const string SearchCriteria = "IsInstalled=0 and Type='Driver'";

    public Task<IReadOnlyList<DriverInfo>> GetDriversAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<DriverInfo>>(() =>
        {
            var drivers = new List<DriverInfo>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT DeviceName, Manufacturer, DriverVersion, DriverDate, DeviceClass FROM Win32_PnPSignedDriver WHERE DeviceName IS NOT NULL");
                foreach (ManagementBaseObject obj in searcher.Get())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    DateTime? date = null;
                    if (obj["DriverDate"] is string raw && raw.Length >= 8)
                    {
                        try
                        {
                            date = ManagementDateTimeConverter.ToDateTime(raw);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Date invalide : laissée vide.
                        }
                    }

                    drivers.Add(new DriverInfo(
                        obj["DeviceName"]?.ToString() ?? string.Empty,
                        obj["Manufacturer"]?.ToString() ?? string.Empty,
                        obj["DriverVersion"]?.ToString() ?? string.Empty,
                        date,
                        obj["DeviceClass"]?.ToString() ?? string.Empty));
                }
            }
            catch (ManagementException)
            {
                // WMI indisponible : liste vide.
            }

            return drivers.OrderBy(d => d.DeviceClass).ThenBy(d => d.DeviceName).ToList();
        }, cancellationToken);
    }

    public Task<IReadOnlyList<DriverUpdateInfo>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<DriverUpdateInfo>>(() =>
        {
            var updates = new List<DriverUpdateInfo>();
            try
            {
                dynamic session = CreateComObject("Microsoft.Update.Session");
                dynamic searcher = session.CreateUpdateSearcher();
                dynamic result = searcher.Search(SearchCriteria);

                foreach (dynamic update in result.Updates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string title = update.Title ?? string.Empty;
                    DateTime? published = null;
                    try
                    {
                        published = (DateTime)update.DriverVerDate;
                    }
                    catch (Exception ex) when (ex is COMException or InvalidCastException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                    {
                        // Date absente : laissée vide.
                    }

                    string driverClass = string.Empty;
                    try
                    {
                        driverClass = update.DriverClass ?? string.Empty;
                    }
                    catch (Exception ex) when (ex is COMException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                    {
                        // Classe absente : rangé dans "Autres".
                    }

                    var versionMatch = VersionRegex().Match(title);
                    updates.Add(new DriverUpdateInfo(
                        (string)update.Identity.UpdateID,
                        title,
                        driverClass,
                        versionMatch.Success ? versionMatch.Value : "—",
                        published));
                }
            }
            catch (Exception ex) when (ex is COMException or InvalidOperationException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                logger.LogWarning(ex, "Windows Update indisponible pour la recherche de pilotes.");
            }

            return updates.OrderBy(u => u.DeviceClass).ThenBy(u => u.Title).ToList();
        }, cancellationToken);
    }

    public Task<string?> InstallUpdatesAsync(IReadOnlyCollection<string> updateIds, CancellationToken cancellationToken = default)
    {
        return Task.Run<string?>(() =>
        {
            try
            {
                dynamic session = CreateComObject("Microsoft.Update.Session");
                dynamic searcher = session.CreateUpdateSearcher();
                dynamic result = searcher.Search(SearchCriteria);
                dynamic toInstall = CreateComObject("Microsoft.Update.UpdateColl");

                foreach (dynamic update in result.Updates)
                {
                    if (updateIds.Contains((string)update.Identity.UpdateID))
                    {
                        if (!(bool)update.EulaAccepted)
                        {
                            update.AcceptEula();
                        }

                        toInstall.Add(update);
                    }
                }

                if ((int)toInstall.Count == 0)
                {
                    return "Les pilotes sélectionnés ne sont plus proposés par Windows Update.";
                }

                dynamic downloader = session.CreateUpdateDownloader();
                downloader.Updates = toInstall;
                downloader.Download();

                dynamic installer = session.CreateUpdateInstaller();
                installer.Updates = toInstall;
                dynamic installResult = installer.Install();

                var code = (int)installResult.ResultCode;
                logger.LogInformation("Installation de {Count} pilote(s) via Windows Update : code {Code}", (int)toInstall.Count, code);
                return code == 2 ? null : $"Windows Update a retourné le code {code}.";
            }
            catch (UnauthorizedAccessException)
            {
                return "Droits administrateur requis : utilisez « Relancer en administrateur ».";
            }
            catch (COMException ex) when ((uint)ex.HResult == 0x80240044 || (uint)ex.HResult == 0x80070005)
            {
                return "Droits administrateur requis : utilisez « Relancer en administrateur ».";
            }
            catch (Exception ex) when (ex is COMException or InvalidOperationException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                logger.LogWarning(ex, "Échec de l'installation de pilotes.");
                return $"Échec de l'installation : {ex.Message}";
            }
        }, cancellationToken);
    }

    private static object CreateComObject(string progId)
    {
        var type = Type.GetTypeFromProgID(progId) ?? throw new InvalidOperationException($"{progId} indisponible.");
        return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"{progId} indisponible.");
    }

    [GeneratedRegex(@"\d+(?:\.\d+){2,}")]
    private static partial Regex VersionRegex();
}
