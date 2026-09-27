using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using V0XCleaner.App.WinUI.Infrastructure;
using V0XCleaner.App.WinUI.ViewModels;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services;
using V0XCleaner.Services.Elevation;
using V0XCleaner.Services.Native;

namespace V0XCleaner.App.WinUI;

public partial class App : Application
{
    private IHost? _host;
    private MainWindow? _window;
    private TrayIconService? _trayIconService;
    private bool _isShuttingDown;

    public App()
    {
        InitializeComponent();
    }

    /// <summary>Résolution DI pour les pages (elles n'ont pas de constructeur injecté par un NavigationService).</summary>
    public static IServiceProvider Services => ((App)Current)._host!.Services;

    /// <summary>Fenêtre principale, utilisée pour appliquer un changement de thème ou obtenir le HWND (ShutdownGuard).</summary>
    public static MainWindow MainWindow => ((App)Current)._window!;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(AppPaths.LogsFolder, "v0xcleaner-winui-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                services.AddV0XCleanerServices();
                // Remplace le NullElevatedOperationClient par défaut : cette app packagée MSIX
                // reste toujours asInvoker, les opérations HKLM/HKCR passent par le helper élevé.
                services.AddSingleton<IElevatedOperationClient, ProcessElevatedOperationClient>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<TrayIconService>();

                // ViewModels des pages portées en Phase 3 (spécifiques WinUI, pas dans AddV0XCleanerServices).
                services.AddTransient<HelpViewModel>();
                services.AddTransient<SystemRestoreViewModel>();
                services.AddTransient<OptionsViewModel>();
                services.AddTransient<QuarantineViewModel>();
                services.AddTransient<StartupManagerViewModel>();
                services.AddTransient<DriversViewModel>();
                services.AddTransient<SoftwareUpdatesViewModel>();
                services.AddTransient<OptimizerViewModel>();
                services.AddTransient<HealthCheckViewModel>();
                services.AddTransient<CleanerPageViewModel>();
                services.AddTransient<RegistryPageViewModel>();
            })
            .Build();

        _host.Start();

        // Empaqueté (MSIX), lancé via l'alias d'exécution stable (AutoCleanScheduler / Phase 6) avec
        // "--silent --clean" : équivalent WinUI du --silent --clean de l'app WPF. L'activation arrive
        // en CommandLineLaunch (et non via les arguments de LaunchActivatedEventArgs, qui ne portent
        // pas la ligne de commande pour ce type d'activation).
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (activationArgs?.Kind == ExtendedActivationKind.CommandLineLaunch
            && activationArgs.Data is Windows.ApplicationModel.Activation.ICommandLineActivatedEventArgs commandLine
            && commandLine.Operation.Arguments.Contains("--silent")
            && commandLine.Operation.Arguments.Contains("--clean"))
        {
            _ = RunSilentCleanAndExitAsync();
            return;
        }

        var settings = _host.Services.GetRequiredService<ISettingsService>();
        var viewModel = _host.Services.GetRequiredService<MainWindowViewModel>();

        _window = new MainWindow(viewModel);
        _window.ApplyTheme(settings.Current.Theme);
        _window.Closed += (_, _) => CompleteShutdown();

        _trayIconService = _host.Services.GetRequiredService<TrayIconService>();
        _trayIconService.ApplySettings();

        _window.Activate();
    }

    /// <summary>
    /// Point de sortie unique de l'application (bouton "Quitter" du menu de la zone de notification,
    /// ou fermeture de la fenêtre quand la surveillance en temps réel est désactivée) : nettoie
    /// (icône de zone de notification, DI, logs) puis termine le processus.
    /// <see cref="Microsoft.UI.Xaml.Application.Exit"/> ne garantit pas le déclenchement de
    /// <see cref="Window.Closed"/> sur les fenêtres restantes, contrairement à WPF — le nettoyage est
    /// donc fait explicitement ici plutôt que dans un OnExit qui pourrait ne jamais s'exécuter.
    /// </summary>
    public static void Shutdown() => ((App)Current).CompleteShutdown();

    /// <summary>Port du RunSilentCleanAndExitAsync de l'app WPF (--silent --clean) : jamais de fenêtre créée.</summary>
    private async Task RunSilentCleanAndExitAsync()
    {
        try
        {
            var catalog = _host!.Services.GetRequiredService<ICleaningCatalog>();
            var logger = _host.Services.GetRequiredService<ILogger<App>>();

            foreach (var task in catalog.GetTasks().Where(t => t.SelectedByDefault))
            {
                try
                {
                    var scanResult = await task.Scanner.ScanAsync();
                    if (scanResult.Items.Count > 0)
                    {
                        await task.Cleaner.CleanAsync(scanResult.Items, OperationMode.Execute);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Échec du nettoyage silencieux pour {Key}", task.Key);
                }
            }
        }
        finally
        {
            Shutdown();
        }
    }

    private void CompleteShutdown()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        _trayIconService?.Dispose();
        _host?.Dispose();
        Log.CloseAndFlush();
        Exit();
    }
}
