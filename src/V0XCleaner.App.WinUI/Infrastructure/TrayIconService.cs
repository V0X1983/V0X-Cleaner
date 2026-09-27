using System.IO;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using Application = Microsoft.UI.Xaml.Application;

namespace V0XCleaner.App.WinUI.Infrastructure;

/// <summary>
/// Icône de zone de notification pour la surveillance en arrière-plan (Étape 6 / Phase 5 WinUI).
/// N'est visible que lorsque l'utilisateur a activé la surveillance dans les Options ; sinon reste
/// jamais créée. Contrairement à WPF (System.Windows.Forms.NotifyIcon), WinUI 3 n'a pas de racine
/// implicite pour héberger l'icône : le <see cref="TaskbarIcon"/> est déclaré comme ressource
/// applicative dans App.xaml et matérialisé ici via ForceCreate() (H.NotifyIcon.WinUI).
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly IJunkEstimator _junkEstimator;
    private readonly ISettingsService _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TrayIconService> _logger;
    private readonly TaskbarIcon _trayIcon;
    private System.Threading.Timer? _timer;
    private bool _created;

    public TrayIconService(IJunkEstimator junkEstimator, ISettingsService settings, IServiceProvider serviceProvider, ILogger<TrayIconService> logger)
    {
        _junkEstimator = junkEstimator;
        _settings = settings;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _trayIcon = (TaskbarIcon)Application.Current.Resources["TrayIcon"];
        _trayIcon.Icon = LoadIcon();
        _trayIcon.DoubleClickCommand = new RelayCommand(ShowMainWindow);

        var openItem = new MenuFlyoutItem { Text = "Ouvrir V0X Cleaner" };
        openItem.Click += (_, _) => ShowMainWindow();

        var quickCleanItem = new MenuFlyoutItem { Text = "Nettoyage rapide" };
        quickCleanItem.Click += (_, _) => _ = QuickCleanAsync();

        var quitItem = new MenuFlyoutItem { Text = "Quitter" };
        quitItem.Click += (_, _) => App.Shutdown();

        _trayIcon.ContextFlyout = new MenuFlyout
        {
            Items =
            {
                openItem,
                quickCleanItem,
                new MenuFlyoutSeparator(),
                quitItem
            }
        };
    }

    /// <summary>
    /// À appeler après chaque changement des Options pour (dés)activer l'icône et la surveillance.
    /// N'appelle <c>ForceCreate()</c> qu'une seule fois pour toute la durée de vie du process : une
    /// fois <see cref="TaskbarIcon"/> détruite via <c>Dispose()</c>, un second <c>ForceCreate()</c>
    /// échoue (<c>TryCreate failed</c>, constaté en pratique en activant/désactivant deux fois de
    /// suite sans relancer l'app). Les activations suivantes basculent donc <c>Show()</c>/<c>Hide()</c>
    /// sur le <see cref="H.NotifyIcon.Core.TrayIcon"/> sous-jacent (réversible), même sémantique que
    /// <c>NotifyIcon.Visible</c> côté WPF ; le vrai <c>Dispose()</c> n'a lieu qu'à la fermeture de
    /// l'application (<see cref="Dispose"/>).
    /// </summary>
    public void ApplySettings()
    {
        if (_settings.Current.RealTimeMonitoringEnabled)
        {
            if (!_created)
            {
                try
                {
                    _trayIcon.ForceCreate(enablesEfficiencyMode: false);
                    _created = true;
                    _logger.LogInformation("Icône de zone de notification créée (IsCreated={IsCreated}).", _trayIcon.IsCreated);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Échec de la création de l'icône de zone de notification.");
                }
            }
            else
            {
                _trayIcon.TrayIcon?.Show();
            }

            var interval = TimeSpan.FromMinutes(Math.Max(5, _settings.Current.MonitoringIntervalMinutes));
            _timer?.Dispose();
            _timer = new System.Threading.Timer(_ => _ = CheckJunkAsync(), null, interval, interval);
        }
        else
        {
            _timer?.Dispose();
            _timer = null;
            if (_created)
            {
                _trayIcon.TrayIcon?.Hide();
            }
        }
    }

    private async Task CheckJunkAsync()
    {
        try
        {
            var bytes = await _junkEstimator.EstimateReclaimableBytesAsync();
            if (bytes >= _settings.Current.MonitoringThresholdBytes)
            {
                _trayIcon.ShowNotification(
                    "V0X Cleaner",
                    $"{ByteFormatter.Format(bytes)} de fichiers temporaires peuvent être nettoyés.",
                    NotificationIcon.Info);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // La surveillance périodique ne doit jamais interrompre l'application.
        }
    }

    private async Task QuickCleanAsync()
    {
        try
        {
            var catalog = _serviceProvider.GetRequiredService<ICleaningCatalog>();
            foreach (var task in catalog.GetTasks().Where(t => t.SelectedByDefault && t.Section == CleaningSection.System))
            {
                var scan = await task.Scanner.ScanAsync();
                if (scan.Items.Count > 0)
                {
                    await task.Cleaner.CleanAsync(scan.Items, OperationMode.Execute);
                }
            }

            _trayIcon.ShowNotification("V0X Cleaner", "Nettoyage rapide terminé.", NotificationIcon.Info);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Charge l'icône directement depuis le disque (System.Drawing.Icon), plutôt que de dépendre de
    /// la conversion asynchrone de <c>TaskbarIcon.IconSource</c> (image WinRT résolue via
    /// <c>ms-appx:///</c>) : constaté en pratique, cette résolution échoue silencieusement
    /// (<c>Icon</c> reste <see langword="null"/>, aucune exception) tant que l'app tourne sans
    /// identité de paquet réelle (double-clic direct sur l'exe, cf. piège de la Phase 0) — Shell_NotifyIcon
    /// enregistre alors une icône invisible, sans erreur. Même contournement que WPF : un vrai fichier
    /// .ico chargé de façon synchrone, indépendant de ms-appx.
    /// </summary>
    private static System.Drawing.Icon LoadIcon()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        return File.Exists(path) ? new System.Drawing.Icon(path) : System.Drawing.SystemIcons.Application;
    }

    private static void ShowMainWindow()
    {
        var window = App.MainWindow;
        window.AppWindow.Show();
        window.Activate();
    }

    public void Dispose()
    {
        _timer?.Dispose();
        if (_created)
        {
            _trayIcon.Dispose();
        }
    }
}
