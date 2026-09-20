using V0XCleaner.Core.Models;

namespace V0XCleaner.Core.Abstractions;

/// <summary>Liste et active/désactive les entrées du menu contextuel ajoutées par des logiciels tiers.</summary>
public interface IContextMenuManager
{
    Task<IReadOnlyList<ContextMenuEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Retourne false en cas d'échec (droits administrateur souvent requis).</summary>
    Task<bool> SetEnabledAsync(ContextMenuEntry entry, bool enabled, CancellationToken cancellationToken = default);
}
