using CommunityToolkit.Mvvm.ComponentModel;
using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

/// <summary>Une ligne cochable de la page Nettoyeur, adossée à une <see cref="CleaningTask"/> du catalogue.</summary>
public partial class CleaningTaskViewModel : ObservableObject
{
    public CleaningTask Task { get; }

    public string DisplayName => Task.DisplayName;

    public string Description => Task.Description;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedSize))]
    private long _foundSizeBytes;

    [ObservableProperty]
    private int _foundItemsCount;

    [ObservableProperty]
    private CleaningTaskStatus _status = CleaningTaskStatus.NotScanned;

    [ObservableProperty]
    private string? _lastError;

    /// <summary>Résultat du dernier scan, conservé pour être transmis tel quel au Cleaner.</summary>
    public IReadOnlyList<CleanupItem> LastScanItems { get; set; } = [];

    public string FormattedSize => ByteFormatter.Format(FoundSizeBytes);

    public CleaningTaskViewModel(CleaningTask task)
    {
        Task = task;
        _isSelected = task.SelectedByDefault;
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
