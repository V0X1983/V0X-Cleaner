using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public sealed class ServiceEntryViewModel(WindowsServiceEntry entry, IWindowsServiceManager manager)
    : ToggleEntryViewModel(entry.StartMode != ServiceStartMode.Disabled)
{
    /// <summary>Type de démarrage à rétablir quand on réactive le service ; « manuel » si le service était déjà désactivé.</summary>
    private ServiceStartMode _modeWhenEnabled = entry.StartMode == ServiceStartMode.Disabled ? ServiceStartMode.Manual : entry.StartMode;

    public WindowsServiceEntry Entry { get; } = entry;

    public string Name => Entry.DisplayName;

    public string Publisher => Entry.Publisher;

    public string FilePath => Entry.FilePath;

    public string StateText => Entry.IsRunning ? "En cours d'exécution" : "Arrêté";

    protected override Task<bool> ApplyEnabledAsync(bool value) =>
        manager.SetStartModeAsync(Entry.Name, value ? _modeWhenEnabled : ServiceStartMode.Disabled);
}
