using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using V0XCleaner.App.Helpers;
using V0XCleaner.App.Infrastructure;
using V0XCleaner.App.ViewModels;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services;
using V0XCleaner.Services.Native;

namespace V0XCleaner.App;

public partial class App : Application
{
    private IHost? _host;
    private TrayIconService? _trayIconService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(AppPaths.LogsFolder, "v0xcleaner-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                services.AddV0XCleanerServices();

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<CleanerPageViewModel>();
                services.AddSingleton<RegistryPageViewModel>();
                services.AddSingleton<StartupManagerViewModel>();
                services.AddSingleton<UninstallManagerViewModel>();
                services.AddSingleton<DiskAnalyzerViewModel>();
                services.AddSingleton<DuplicateFinderViewModel>();
                services.AddSingleton<DriveWiperViewModel>();
                services.AddSingleton<SystemRestoreViewModel>();
                services.AddSingleton<HealthCheckViewModel>();
                services.AddSingleton<ToolsPageViewModel>();
                services.AddSingleton<OptionsViewModel>();
                services.AddSingleton<TrayIconService>();
            })
            .Build();

        _host.Start();

        var settings = _host.Services.GetRequiredService<ISettingsService>();
        ThemeManager.ApplyTheme(settings.Current.Theme);

        if (e.Args.Contains("--silent") && e.Args.Contains("--clean"))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = RunSilentCleanAndExitAsync();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _trayIconService = _host.Services.GetRequiredService<TrayIconService>();
        _trayIconService.ApplySettings();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Closing += (_, args) =>
        {
            if (settings.Current.RealTimeMonitoringEnabled)
            {
                args.Cancel = true;
                mainWindow.Hide();
            }
            else
            {
                Shutdown();
            }
        };

        mainWindow.Show();
    }

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
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIconService?.Dispose();
        _host?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
