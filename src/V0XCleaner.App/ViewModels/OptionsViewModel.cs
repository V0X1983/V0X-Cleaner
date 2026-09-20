using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.Helpers;
using V0XCleaner.App.Infrastructure;
using V0XCleaner.Core.Abstractions;

namespace V0XCleaner.App.ViewModels;

public partial class OptionsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IAutoCleanScheduler _scheduler;
    private readonly IUpdateChecker _updateChecker;
    private readonly TrayIconService _trayIconService;
    private readonly ILogger<OptionsViewModel> _logger;

    public IReadOnlyList<string> FrequencyOptions { get; } = ["Daily", "Weekly"];

    public IReadOnlyList<int> HourOptions { get; } = Enumerable.Range(0, 24).ToList();

    public IReadOnlyList<string> ThemeOptions { get; } = ["Dark", "Light"];

    [ObservableProperty]
    private bool _autoCleanEnabled;

    [ObservableProperty]
    private string _autoCleanFrequency;

    [ObservableProperty]
    private int _autoCleanHour;

    [ObservableProperty]
    private bool _realTimeMonitoringEnabled;

    [ObservableProperty]
    private int _monitoringIntervalMinutes;

    [ObservableProperty]
    private int _monitoringThresholdMb;

    [ObservableProperty]
    private string _updateCheckOwner;

    [ObservableProperty]
    private string _updateCheckRepo;

    [ObservableProperty]
    private string? _updateStatusMessage;

    [ObservableProperty]
    private string? _latestReleaseUrl;

    [ObservableProperty]
    private string _selectedTheme;

    [ObservableProperty]
    private bool _quarantineEnabled;

    [ObservableProperty]
    private int _quarantineRetentionDays;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Modifiez les paramètres puis cliquez sur \"Enregistrer\".";

    public OptionsViewModel(
        ISettingsService settings,
        IAutoCleanScheduler scheduler,
        IUpdateChecker updateChecker,
        TrayIconService trayIconService,
        ILogger<OptionsViewModel> logger)
    {
        _settings = settings;
        _scheduler = scheduler;
        _updateChecker = updateChecker;
        _trayIconService = trayIconService;
        _logger = logger;

        var current = settings.Current;
        _autoCleanEnabled = current.AutoCleanEnabled;
        _autoCleanFrequency = current.AutoCleanFrequency;
        _autoCleanHour = current.AutoCleanHour;
        _realTimeMonitoringEnabled = current.RealTimeMonitoringEnabled;
        _monitoringIntervalMinutes = current.MonitoringIntervalMinutes;
        _monitoringThresholdMb = (int)(current.MonitoringThresholdBytes / (1024 * 1024));
        _updateCheckOwner = current.UpdateCheckOwner;
        _updateCheckRepo = current.UpdateCheckRepo;
        _selectedTheme = current.Theme;
        _quarantineEnabled = current.QuarantineEnabled;
        _quarantineRetentionDays = current.QuarantineRetentionDays;
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task SaveAsync()
    {
        IsBusy = true;
        StatusMessage = "Enregistrement des paramètres...";

        try
        {
            var current = _settings.Current;
            current.AutoCleanEnabled = AutoCleanEnabled;
            current.AutoCleanFrequency = AutoCleanFrequency;
            current.AutoCleanHour = AutoCleanHour;
            current.RealTimeMonitoringEnabled = RealTimeMonitoringEnabled;
            current.MonitoringIntervalMinutes = MonitoringIntervalMinutes;
            current.MonitoringThresholdBytes = (long)MonitoringThresholdMb * 1024 * 1024;
            current.UpdateCheckOwner = UpdateCheckOwner;
            current.UpdateCheckRepo = UpdateCheckRepo;
            current.Theme = SelectedTheme;
            current.QuarantineEnabled = QuarantineEnabled;
            current.QuarantineRetentionDays = Math.Max(1, QuarantineRetentionDays);

            await _settings.SaveAsync();

            var scheduled = await Task.Run(() => _scheduler.ConfigureAsync(AutoCleanEnabled, AutoCleanFrequency, AutoCleanHour));
            _trayIconService.ApplySettings();
            ThemeManager.ApplyTheme(SelectedTheme);

            StatusMessage = scheduled
                ? "Paramètres enregistrés."
                : "Paramètres enregistrés, mais la planification automatique n'a pas pu être configurée (Planificateur de tâches indisponible).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'enregistrement des paramètres.");
            StatusMessage = "Erreur lors de l'enregistrement des paramètres.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CheckForUpdatesAsync()
    {
        IsBusy = true;
        UpdateStatusMessage = "Vérification en cours...";
        LatestReleaseUrl = null;

        try
        {
            var result = await Task.Run(() => _updateChecker.GetLatestReleaseAsync(UpdateCheckOwner, UpdateCheckRepo));
            UpdateStatusMessage = result.Message;
            LatestReleaseUrl = result.ReleaseUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification des mises à jour.");
            UpdateStatusMessage = "Erreur lors de la vérification des mises à jour.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
    }
}
