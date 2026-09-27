using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Serilog;
using V0XCleaner.App.WinUI.Infrastructure;
using V0XCleaner.App.WinUI.ViewModels;
using V0XCleaner.Core.Abstractions;
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
