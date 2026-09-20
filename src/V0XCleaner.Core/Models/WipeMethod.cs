namespace V0XCleaner.Core.Models;

/// <summary>
/// Méthode d'effacement de l'espace libre. Le Gutmann (35 passes) est un vestige des disques
/// magnétiques des années 1990 : inutile sur un SSD moderne, juste beaucoup plus lent.
/// </summary>
public enum WipeMethod
{
    SinglePassZero,
    Dod3Pass,
    Gutmann35Pass
}
