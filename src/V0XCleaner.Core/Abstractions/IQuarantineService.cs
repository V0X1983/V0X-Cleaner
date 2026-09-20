using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>
/// "Corbeille de sécurité" de V0X Cleaner (Étape 7 — Undo) : au lieu de supprimer définitivement
/// un fichier détecté par le Nettoyeur ou le chercheur de doublons, on le déplace en quarantaine,
/// ce qui permet de le restaurer en cas d'erreur. Les entrées plus vieilles que la rétention
/// configurée sont purgées automatiquement (voir <see cref="PurgeExpired"/>).
/// </summary>
public interface IQuarantineService
{
    /// <summary>Déplace le fichier vers la quarantaine. Retourne l'entrée créée, ou null en cas d'échec (le fichier n'existe pas, verrouillé, permissions...).</summary>
    QuarantineEntry? Quarantine(string path);

    /// <summary>Écrit l'index sur disque. La mise en quarantaine regroupe les écritures (toutes les 200 entrées) : à appeler en fin de lot.</summary>
    void Flush();

    /// <summary>Liste les fichiers actuellement en quarantaine, du plus récent au plus ancien.</summary>
    IReadOnlyList<QuarantineEntry> GetEntries();

    /// <summary>Replace un fichier en quarantaine à son emplacement d'origine. Échoue si un fichier existe déjà à cet endroit.</summary>
    bool Restore(string entryId, out string? error);

    /// <summary>Supprime définitivement une entrée de la quarantaine (vidage manuel).</summary>
    bool PurgeEntry(string entryId);

    /// <summary>Supprime définitivement toutes les entrées plus vieilles que <paramref name="retention"/>. Retourne le nombre d'entrées purgées.</summary>
    int PurgeExpired(TimeSpan retention, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
