using System.IO;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Abstractions;
using Application = System.Windows.Application;

namespace V0XCleaner.App.Infrastructure;

/// <summary>
/// Icône de zone de notification pour la surveillance en arrière-plan (Étape 6). N'est visible
/// que lorsque l'utilisateur a activé la surveillance dans les Options ; sinon reste invisible
/// et inactive. WPF n'a pas d'API native pour une icône système : on s'appuie sur
/// System.Windows.Forms.NotifyIcon (interop WinForms), approche standard pour ce besoin en WPF.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private readonly IJunkEstimator _junkEstimator;
    private readonly ISettingsService _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly NotifyIcon _notifyIcon;
    private System.Threading.Timer? _timer;

    public TrayIconService(IJunkEstimator junkEstimator, ISettingsService settings, IServiceProvider serviceProvider)
    {
        _junkEstimator = junkEstimator;
        _settings = settings;
        _serviceProvider = serviceProvider;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Ouvrir V0X Cleaner", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Nettoyage rapide", null, (_, _) => _ = QuickCleanAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => Application.Current.Shutdown());

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "V0X Cleaner",
            ContextMenuStrip = menu,
            Visible = false
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private static System.Drawing.Icon LoadIcon()
    {
        var stream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/app.ico"))?.Stream;
        return stream is null ? System.Drawing.SystemIcons.Application : new System.Drawing.Icon(stream);
    }

    /// <summary>À appeler après chaque changement des Options pour (dés)activer l'icône et la surveillance.</summary>
    public void ApplySettings()
    {
        if (_settings.Current.RealTimeMonitoringEnabled)
        {
            _notifyIcon.Visible = true;
            var interval = TimeSpan.FromMinutes(Math.Max(5, _settings.Current.MonitoringIntervalMinutes));
            _timer?.Dispose();
            _timer = new System.Threading.Timer(_ => _ = CheckJunkAsync(), null, interval, interval);
        }
        else
        {
            _timer?.Dispose();
            _timer = null;
            _notifyIcon.Visible = false;
        }
    }

    private async Task CheckJunkAsync()
    {
        try
        {
            var bytes = await _junkEstimator.EstimateReclaimableBytesAsync();
            if (bytes >= _settings.Current.MonitoringThresholdBytes)
            {
                _notifyIcon.BalloonTipTitle = "V0X Cleaner";
                _notifyIcon.BalloonTipText = $"{ByteFormatter.Format(bytes)} de fichiers temporaires peuvent être nettoyés.";
                _notifyIcon.ShowBalloonTip(8000);
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
            foreach (var task in catalog.GetTasks().Where(t => t.SelectedByDefault && t.Section == Core.Models.CleaningSection.System))
            {
                var scan = await task.Scanner.ScanAsync();
                if (scan.Items.Count > 0)
                {
                    await task.Cleaner.CleanAsync(scan.Items, Core.Models.OperationMode.Execute);
                }
            }

            _notifyIcon.BalloonTipTitle = "V0X Cleaner";
            _notifyIcon.BalloonTipText = "Nettoyage rapide terminé.";
            _notifyIcon.ShowBalloonTip(5000);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private void ShowMainWindow()
    {
        var window = _serviceProvider.GetRequiredService<MainWindow>();
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _notifyIcon.Dispose();
    }
}
