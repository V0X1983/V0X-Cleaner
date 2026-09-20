using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.ViewModels;

public partial class HomePageViewModel : ObservableObject
{
    private readonly IHealthCheckService _health;
    private readonly IJunkEstimator _junk;
    private readonly IQuarantineService _quarantine;
    private readonly ISettingsService _settings;
    private readonly ILogger<HomePageViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _healthScoreText = "—";

    [ObservableProperty]
    private string _reclaimableText = "—";

    [ObservableProperty]
    private string _lastCleanText = "Aucun nettoyage effectué";

    [ObservableProperty]
    private string _quarantineText = "—";

    public HomePageViewModel(
        IHealthCheckService health,
        IJunkEstimator junk,
        IQuarantineService quarantine,
        ISettingsService settings,
        ILogger<HomePageViewModel> logger)
    {
        _health = health;
        _junk = junk;
        _quarantine = quarantine;
        _settings = settings;
        _logger = logger;
        UpdateLocalInfo();
    }

    private void UpdateLocalInfo()
    {
        var current = _settings.Current;
        LastCleanText = current.LastCleanUtc is { } when
            ? $"{when.ToLocalTime():dd/MM/yyyy HH:mm} — {ByteFormatter.Format(current.LastCleanFreedBytes)} libérés"
            : "Aucun nettoyage effectué";

        var entries = _quarantine.GetEntries();
        QuarantineText = $"{entries.Count} fichier(s), {ByteFormatter.Format(entries.Sum(e => e.SizeBytes))}";
    }

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        HealthScoreText = "…";
        ReclaimableText = "…";
        UpdateLocalInfo();

        try
        {
            var healthTask = Task.Run(() => _health.RunAsync());
            var junkTask = Task.Run(() => _junk.EstimateReclaimableBytesAsync());
            HealthScoreText = $"{(await healthTask).OverallScore}/100";
            ReclaimableText = ByteFormatter.Format(await junkTask);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du calcul du tableau de bord.");
            HealthScoreText = "Erreur";
            ReclaimableText = "Erreur";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRefresh() => !IsBusy;

    partial void OnIsBusyChanged(bool value) => RefreshCommand.NotifyCanExecuteChanged();
}
