using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using V0XCleaner.App.WinUI.Infrastructure;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Services.Native;

namespace V0XCleaner.App.WinUI.ViewModels;

public partial class OptionsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IAutoCleanScheduler _scheduler;
    private readonly IUpdateChecker _updateChecker;
    private readonly ILogger<OptionsViewModel> _logger;
    private readonly TrayIconService _trayIconService;

    public IReadOnlyList<string> FrequencyOptions { get; } = ["Daily", "Weekly"];

    public IReadOnlyList<int> HourOptions { get; } = Enumerable.Range(0, 24).ToList();

    public IReadOnlyList<string> ThemeOptions { get; } = ["Dark", "Light"];

    [ObservableProperty]
    public partial bool AutoCleanEnabled { get; set; }

    [ObservableProperty]
    public partial string AutoCleanFrequency { get; set; }

    [ObservableProperty]
    public partial int AutoCleanHour { get; set; }

    [ObservableProperty]
    public partial bool RealTimeMonitoringEnabled { get; set; }

    [ObservableProperty]
    public partial int MonitoringIntervalMinutes { get; set; }

    [ObservableProperty]
    public partial int MonitoringThresholdMb { get; set; }

    [ObservableProperty]
    public partial string UpdateCheckOwner { get; set; }

    [ObservableProperty]
    public partial string UpdateCheckRepo { get; set; }

    [ObservableProperty]
    public partial string? UpdateStatusMessage { get; set; }

    [ObservableProperty]
    public partial string? LatestReleaseUrl { get; set; }

    [ObservableProperty]
    public partial string SelectedTheme { get; set; }

    [ObservableProperty]
    public partial string ExcludedPathsText { get; set; }

    [ObservableProperty]
    public partial bool StartWithWindows { get; set; }

    public string AboutText { get; } =
        "V0X Cleaner v" + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0");

    [ObservableProperty]
    public partial bool QuarantineEnabled { get; set; }

    [ObservableProperty]
    public partial int QuarantineRetentionDays { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Modifiez les paramètres puis cliquez sur \"Enregistrer\".";

    public OptionsViewModel(
        ISettingsService settings,
        IAutoCleanScheduler scheduler,
        IUpdateChecker updateChecker,
        ILogger<OptionsViewModel> logger,
        TrayIconService trayIconService)
    {
        _settings = settings;
        _scheduler = scheduler;
        _updateChecker = updateChecker;
        _logger = logger;
        _trayIconService = trayIconService;

        var current = settings.Current;
        AutoCleanEnabled = current.AutoCleanEnabled;
        AutoCleanFrequency = current.AutoCleanFrequency;
        AutoCleanHour = current.AutoCleanHour;
        RealTimeMonitoringEnabled = current.RealTimeMonitoringEnabled;
        MonitoringIntervalMinutes = current.MonitoringIntervalMinutes;
        MonitoringThresholdMb = (int)(current.MonitoringThresholdBytes / (1024 * 1024));
        UpdateCheckOwner = current.UpdateCheckOwner;
        UpdateCheckRepo = current.UpdateCheckRepo;
        SelectedTheme = current.Theme;
        ExcludedPathsText = string.Join(Environment.NewLine, current.ExcludedPaths);
        StartWithWindows = current.StartWithWindows;
        QuarantineEnabled = current.QuarantineEnabled;
        QuarantineRetentionDays = current.QuarantineRetentionDays;
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
            App.MainWindow.ApplyTheme(SelectedTheme);
            _trayIconService.ApplySettings();

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
            if (!Version.TryParse(result.Version, out var latest) || installed is null || latest <= new Version(installed.Major, installed.Minor, Math.Max(installed.Build, 0)))
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

        Microsoft.UI.Xaml.Application.Current.Exit();
    }

    private bool CanRun() => !IsBusy;

    partial void OnIsBusyChanged(bool value)
    {
        SaveCommand.NotifyCanExecuteChanged();
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
    }
}
