namespace V0XCleaner.Core.Models;

/// <summary>
/// Contrôle si une opération destructive (nettoyage, réparation registre, effacement disque...)
/// se contente de calculer son effet (Simulate) ou l'applique réellement (Execute).
/// Toute nouvelle fonctionnalité touchant au système doit respecter ce mode.
/// </summary>
public enum OperationMode
{
    Simulate,
    Execute
}
