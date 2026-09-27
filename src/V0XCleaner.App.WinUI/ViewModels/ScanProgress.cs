using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace V0XCleaner.App.WinUI.ViewModels;

/// <summary>État de l'écran de chargement plein écran partagé par tous les écrans « Analyser » (voir Controls/LoadingOverlay).</summary>
public partial class ScanProgress : ObservableObject
{
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool CanStop { get; set; } = true;

    [ObservableProperty]
    public partial string StopLabel { get; set; } = "Arrêter l'analyse";

    /// <summary>Pourcentage 0-100, ou -1 quand la durée est inconnue (anneau animé sans chiffre).</summary>
    [ObservableProperty]
    public partial double Progress { get; set; } = -1;

    public CancellationToken Begin(string message, string stopLabel = "Arrêter l'analyse")
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        Message = message;
        StopLabel = stopLabel;
        Progress = -1;
        IsActive = true;
        return _cts.Token;
    }

    public void End()
    {
        IsActive = false;
        _cts?.Dispose();
        _cts = null;
    }

    [RelayCommand]
    private void Stop() => _cts?.Cancel();
}
