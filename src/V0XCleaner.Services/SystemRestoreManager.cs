using System.Management;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services;

/// <summary>
/// Liste, crée et supprime des points de restauration système via la classe WMI SystemRestore
/// (espace de noms root\default). Échoue silencieusement (liste vide / échec explicite) si la
/// protection du système est désactivée plutôt que de lever une exception.
/// </summary>
public sealed class SystemRestoreManager : ISystemRestoreManager
{
    private const int EventTypeBeginSystemChange = 100;
    private const int RestorePointTypeApplicationInstall = 0;

    public Task<IReadOnlyList<RestorePointInfo>> GetRestorePointsAsync(CancellationToken cancellationToken = default)
    {
        var points = new List<RestorePointInfo>();

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\default", "SELECT * FROM SystemRestore");
            using var results = searcher.Get();

            foreach (ManagementBaseObject obj in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (obj)
                {
                    var creationRaw = obj["CreationTime"] as string;
                    points.Add(new RestorePointInfo
                    {
                        SequenceNumber = Convert.ToInt32(obj["SequenceNumber"]),
                        Description = obj["Description"] as string ?? "(sans nom)",
                        CreationTime = string.IsNullOrWhiteSpace(creationRaw)
                            ? DateTime.MinValue
                            : ManagementDateTimeConverter.ToDateTime(creationRaw),
                        RestorePointType = Convert.ToInt32(obj["RestorePointType"])
                    });
                }
            }
        }
        catch (ManagementException)
        {
            // Protection du système désactivée, ou WMI indisponible : on renvoie une liste vide.
        }

        return Task.FromResult<IReadOnlyList<RestorePointInfo>>(points.OrderByDescending(p => p.CreationTime).ToList());
    }

    public Task<OperationOutcome> CreateRestorePointAsync(string description, CancellationToken cancellationToken = default)
    {
        try
        {
            using var restoreClass = new ManagementClass(@"root\default", "SystemRestore", null);
            using var inParams = restoreClass.GetMethodParameters("CreateRestorePoint");
            inParams["Description"] = description;
            inParams["EventType"] = EventTypeBeginSystemChange;
            inParams["RestorePointType"] = RestorePointTypeApplicationInstall;

            using var result = restoreClass.InvokeMethod("CreateRestorePoint", inParams, null);
            var returnValue = result is not null ? Convert.ToInt32(result["ReturnValue"]) : -1;

            return Task.FromResult(returnValue == 0
                ? new OperationOutcome(true, "Point de restauration créé.")
                : new OperationOutcome(false, $"Échec de la création du point de restauration (code {returnValue})."));
        }
        catch (ManagementException ex)
        {
            return Task.FromResult(new OperationOutcome(false, ex.Message));
        }
    }

    public Task<OperationOutcome> DeleteRestorePointAsync(int sequenceNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = SrClientInterop.SRRemoveRestorePoint((uint)sequenceNumber);
            return Task.FromResult(result == 0
                ? new OperationOutcome(true, "Point de restauration supprimé.")
                : new OperationOutcome(false, $"Échec de la suppression (code {result})."));
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return Task.FromResult(new OperationOutcome(false, "Fonction de suppression indisponible sur ce système."));
        }
    }
}
