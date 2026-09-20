using Microsoft.Extensions.DependencyInjection;

namespace V0XCleaner.Services;

/// <summary>
/// Point d'enregistrement unique des services (scanners, cleaners, accès système).
/// Chaque étape de la roadmap ajoute ses enregistrements ici plutôt que dans App.xaml.cs.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddV0XCleanerServices(this IServiceCollection services)
    {
        // Les scanners/cleaners concrets seront enregistrés ici au fil des étapes
        // (Étape 1 : nettoyage système, Étape 2 : registre, etc.).
        return services;
    }
}
