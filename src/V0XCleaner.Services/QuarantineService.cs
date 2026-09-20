using System.Security;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.Native;

namespace V0XCleaner.Services;

/// <summary>
/// Implémentation fichier de la "corbeille de sécurité" : chaque fichier mis en quarantaine est
/// déplacé dans un sous-dossier dédié (identifié par un GUID) sous <see cref="AppPaths.QuarantineFolder"/>,
/// et un index JSON garde la trace de son emplacement d'origine pour permettre la restauration.
/// </summary>
public sealed class QuarantineService(ILogger<QuarantineService> logger, string? rootFolder = null) : IQuarantineService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly object _lock = new();
    private List<QuarantineEntry>? _cache;
    private int _unsaved;

    private const int FlushEvery = 200;

    private string Root => rootFolder is null ? AppPaths.QuarantineFolder : Directory.CreateDirectory(rootFolder).FullName;

    private string IndexFilePath => Path.Combine(Root, "index.json");

    public QuarantineEntry? Quarantine(string path)
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                var sizeBytes = new FileInfo(path).Length;
                var id = Guid.NewGuid().ToString("N");
                var entryFolder = Path.Combine(Root, id);
                Directory.CreateDirectory(entryFolder);

                var destination = Path.Combine(entryFolder, Path.GetFileName(path));
                File.Move(path, destination);

                var entry = new QuarantineEntry
                {
                    Id = id,
                    OriginalPath = path,
                    QuarantinedPath = destination,
                    SizeBytes = sizeBytes,
                    QuarantinedAtUtc = DateTime.UtcNow
                };

                var entries = LoadIndex();
                entries.Add(entry);
                if (++_unsaved >= FlushEvery)
                {
                    SaveIndex(entries);
                }

                return entry;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                logger.LogWarning(ex, "Échec de la mise en quarantaine de {Path}", path);
                return null;
            }
        }
    }

    public void Flush()
    {
        lock (_lock)
        {
            if (_unsaved > 0)
            {
                SaveIndex(LoadIndex());
            }
        }
    }

    public IReadOnlyList<QuarantineEntry> GetEntries()
    {
        lock (_lock)
        {
            return LoadIndex().OrderByDescending(e => e.QuarantinedAtUtc).ToList();
        }
    }

    public bool Restore(string entryId, out string? error)
    {
        lock (_lock)
        {
            var entries = LoadIndex();
            var entry = entries.FirstOrDefault(e => e.Id == entryId);
            if (entry is null)
            {
                error = "Entrée de quarantaine introuvable.";
                return false;
            }

            if (File.Exists(entry.OriginalPath))
            {
                error = "Un fichier existe déjà à l'emplacement d'origine.";
                return false;
            }

            try
            {
                var directory = Path.GetDirectoryName(entry.OriginalPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Move(entry.QuarantinedPath, entry.OriginalPath);

                entries.Remove(entry);
                SaveIndex(entries);
                TryDeleteEntryFolder(entry.Id);

                error = null;
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                logger.LogWarning(ex, "Échec de la restauration de {Path}", entry.OriginalPath);
                error = ex.Message;
                return false;
            }
        }
    }

    public bool PurgeEntry(string entryId)
    {
        lock (_lock)
        {
            var entries = LoadIndex();
            var entry = entries.FirstOrDefault(e => e.Id == entryId);
            if (entry is null)
            {
                return false;
            }

            TryDeleteEntryFolder(entry.Id);
            entries.Remove(entry);
            SaveIndex(entries);
            return true;
        }
    }

    public int PurgeExpired(TimeSpan retention)
    {
        lock (_lock)
        {
            var entries = LoadIndex();
            var cutoff = DateTime.UtcNow - retention;
            var expired = entries.Where(e => e.QuarantinedAtUtc < cutoff).ToList();

            foreach (var entry in expired)
            {
                TryDeleteEntryFolder(entry.Id);
                entries.Remove(entry);
            }

            if (expired.Count > 0)
            {
                SaveIndex(entries);
                logger.LogInformation("{Count} élément(s) purgé(s) de la quarantaine (rétention dépassée).", expired.Count);
            }

            return expired.Count;
        }
    }

    private void TryDeleteEntryFolder(string id)
    {
        try
        {
            var folder = Path.Combine(Root, id);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Échec de la suppression du dossier de quarantaine {Id}", id);
        }
    }

    private List<QuarantineEntry> LoadIndex()
    {
        return _cache ??= ReadIndexFromDisk();
    }

    private List<QuarantineEntry> ReadIndexFromDisk()
    {
        try
        {
            if (File.Exists(IndexFilePath))
            {
                var json = File.ReadAllText(IndexFilePath);
                var entries = JsonSerializer.Deserialize<List<QuarantineEntry>>(json);
                if (entries is not null)
                {
                    return entries;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Index de quarantaine illisible, réinitialisation.");
        }

        return [];
    }

    private void SaveIndex(List<QuarantineEntry> entries)
    {
        try
        {
            var json = JsonSerializer.Serialize(entries, JsonOptions);
            File.WriteAllText(IndexFilePath, json);
            _unsaved = 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Échec de l'écriture de l'index de quarantaine.");
        }
    }
}
