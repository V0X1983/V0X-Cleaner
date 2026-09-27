using CommunityToolkit.Mvvm.ComponentModel;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.WinUI.ViewModels;

/// <summary>Une ligne cochable des pages Nettoyeur/Registre, adossée à une <see cref="CleaningTask"/> du catalogue.</summary>
public partial class CleaningTaskViewModel : ObservableObject
{
    public CleaningTask Task { get; }

    public string DisplayName => Task.DisplayName;

    public string Description => Task.Description;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedSize))]
    public partial long FoundSizeBytes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FoundItemsLabel))]
    public partial int FoundItemsCount { get; set; }

    [ObservableProperty]
    public partial CleaningTaskStatus Status { get; set; } = CleaningTaskStatus.NotScanned;

    [ObservableProperty]
    public partial string? LastError { get; set; }

    /// <summary>Résultat du dernier scan, conservé pour être transmis tel quel au Cleaner.</summary>
    public IReadOnlyList<CleanupItem> LastScanItems { get; set; } = [];

    public string FormattedSize => ByteFormatter.Format(FoundSizeBytes);

    /// <summary>Remplace le binding StringFormat WPF (non supporté en WinUI), utilisé par la page Registre.</summary>
    public string FoundItemsLabel => $"{FoundItemsCount} élément(s)";

    public CleaningTaskViewModel(CleaningTask task)
    {
        Task = task;
        IsSelected = task.SelectedByDefault;
    }

    public void ResetScanState()
    {
        FoundSizeBytes = 0;
        FoundItemsCount = 0;
        LastScanItems = [];
        Status = CleaningTaskStatus.NotScanned;
        LastError = null;
    }
}
