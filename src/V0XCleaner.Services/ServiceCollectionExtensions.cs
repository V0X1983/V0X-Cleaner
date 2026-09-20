using Microsoft.Extensions.DependencyInjection;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Services.FileSystem;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Services;

/// <summary>
/// Point d'enregistrement unique des services (scanners, cleaners, accès système).
/// Chaque étape de la roadmap ajoute ses enregistrements ici plutôt que dans App.xaml.cs.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddV0XCleanerServices(this IServiceCollection services)
    {
        services.AddSingleton<IPathGuard, PathGuard>();
        services.AddSingleton<ICleaningCatalog, CleaningCatalog>();

        services.AddSingleton<IRegistryBackupService, RegistryBackupService>();
        services.AddSingleton<IRegistryIssueCatalog, RegistryIssueCatalog>();

        services.AddSingleton<IStartupManager, StartupManager>();

        services.AddSingleton<IInstalledProgramsCatalog, InstalledProgramsCatalog>();

        services.AddSingleton<IDiskAnalyzer, DiskAnalyzer>();
        services.AddSingleton<IDuplicateFileFinder, DuplicateFileFinder>();
        services.AddSingleton<IDriveWiper, DriveWiper>();
        services.AddSingleton<ISystemRestoreManager, SystemRestoreManager>();

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAutoCleanScheduler, AutoCleanScheduler>();
        services.AddSingleton<IUpdateChecker, GitHubUpdateChecker>();
        services.AddSingleton<IJunkEstimator, JunkEstimator>();
        services.AddSingleton<IMemoryOptimizer, MemoryOptimizer>();
        services.AddSingleton<IHealthCheckService, HealthCheckService>();

        return services;
    }
}
