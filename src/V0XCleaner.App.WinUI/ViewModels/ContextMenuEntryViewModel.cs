using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.WinUI.ViewModels;

public sealed class ContextMenuEntryViewModel(ContextMenuEntry entry, IContextMenuManager manager) : ToggleEntryViewModel(entry.IsEnabled)
{
    public ContextMenuEntry Entry { get; } = entry;

    public string Name => Entry.Name;

    public string Publisher => Entry.Publisher;

    public string FilePath => Entry.FilePath;

    protected override string FailureMessage => Entry.Kind == ContextMenuEntryKind.Handler
        ? "Échec : les extensions se bloquent en administrateur."
        : "Échec (droits administrateur peut-être requis).";

    protected override Task<bool> ApplyEnabledAsync(bool value) => manager.SetEnabledAsync(Entry, value);
}

public sealed class ContextMenuGroupViewModel(string title, string glyph, IReadOnlyList<ContextMenuEntryViewModel> entries)
{
    public string Title => $"{title} ({Entries.Count})";

    public string Glyph { get; } = glyph;

    public IReadOnlyList<ContextMenuEntryViewModel> Entries { get; } = entries;
}
