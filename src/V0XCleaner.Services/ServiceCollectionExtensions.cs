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

        return services;
    }
}
