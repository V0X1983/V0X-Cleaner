using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.Helpers;
using V0XCleaner.App.Infrastructure;
using V0XCleaner.Core;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Services.Native;

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
    private string _excludedPathsText;

    [ObservableProperty]
    private bool _startWithWindows;

    public string AboutText { get; } =
        "V0X Cleaner v" + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0");

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
        _excludedPathsText = string.Join(Environment.NewLine, current.ExcludedPaths);
        _startWithWindows = current.StartWithWindows;
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
            current.ExcludedPaths = ExcludedPathsText
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            current.StartWithWindows = StartWithWindows;
            var startupOk = WindowsStartupRegistration.Apply(StartWithWindows);
            current.QuarantineEnabled = QuarantineEnabled;
            current.QuarantineRetentionDays = Math.Max(1, QuarantineRetentionDays);

            await _settings.SaveAsync();

            var scheduled = await Task.Run(() => _scheduler.ConfigureAsync(AutoCleanEnabled, AutoCleanFrequency, AutoCleanHour));
            _trayIconService.ApplySettings();
            ThemeManager.ApplyTheme(SelectedTheme);

            StatusMessage = !startupOk
                ? "Paramètres enregistrés, mais le lancement avec Windows n'a pas pu être configuré."
                : scheduled
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

            if (!result.Success)
            {
                return;
            }

            var installed = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (!UpdateVersionComparer.IsNewer(result.Version, installed))
            {
                UpdateStatusMessage = $"V0X Cleaner est à jour (version {installed?.ToString(3)}).";
                return;
            }

            if (string.IsNullOrWhiteSpace(result.InstallerUrl))
            {
                UpdateStatusMessage = $"La version {result.Version} est disponible, mais aucun installeur n'y est joint : utilisez « Ouvrir la version ».";
                return;
            }

            UpdateStatusMessage = $"Version {result.Version} trouvée. Téléchargement...";
            var progress = new Progress<double>(p => UpdateStatusMessage = $"Version {result.Version} : téléchargement {p:P0}...");
            var installerPath = await Task.Run(() => _updateChecker.DownloadInstallerAsync(result, progress));

            UpdateStatusMessage = "Installation de la mise à jour : V0X Cleaner va se fermer puis se relancer.";
            LaunchInstallerAndRestart(installerPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification des mises à jour.");
            UpdateStatusMessage = $"Erreur lors de la mise à jour : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Lance l'installeur en silencieux (avec élévation UAC) puis relance l'application, dans un
    /// PowerShell indépendant qui survit à la fermeture de V0X Cleaner. Si l'élévation est refusée,
    /// l'application se relance quand même, sans mise à jour.
    /// </summary>
    private static void LaunchInstallerAndRestart(string installerPath)
    {
        static string Q(string value) => value.Replace("'", "''");

        var appPath = Environment.ProcessPath ?? string.Empty;
        var script =
            $"try {{ Start-Process -FilePath '{Q(installerPath)}' -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CLOSEAPPLICATIONS' -Verb RunAs -Wait }} catch {{ }}; " +
            $"Start-Process -FilePath '{Q(appPath)}'";

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell.exe",
            $"-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -Command \"{script}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });

        System.Windows.Application.Current.Shutdown();
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
    }
}
