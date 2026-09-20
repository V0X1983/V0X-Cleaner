namespace V0XCleaner.Core.Models;

/// <summary>Paramètres persistants de l'application, sérialisés en JSON dans %AppData%\V0XCleaner\config.json.</summary>
public sealed class AppSettings
{
    public bool AutoCleanEnabled { get; set; }
    public string AutoCleanFrequency { get; set; } = "Daily";
    public int AutoCleanHour { get; set; } = 3;

    public bool RealTimeMonitoringEnabled { get; set; }
    public int MonitoringIntervalMinutes { get; set; } = 60;
    public long MonitoringThresholdBytes { get; set; } = 500L * 1024 * 1024;

    /// <summary>Dépôt GitHub Releases vérifié pour les mises à jour ; les valeurs vides retombent sur le dépôt officiel.</summary>
    public const string DefaultUpdateOwner = "v0x83";
    public const string DefaultUpdateRepo = "V0X-Cleaner";

    public string UpdateCheckOwner { get; set; } = DefaultUpdateOwner;
    public string UpdateCheckRepo { get; set; } = DefaultUpdateRepo;

    public string Theme { get; set; } = "Dark";

    public DateTime? LastCleanUtc { get; set; }
    public long LastCleanFreedBytes { get; set; }

    /// <summary>Chemins (fichier ou dossier) jamais nettoyés.</summary>
    public List<string> ExcludedPaths { get; set; } = [];

    /// <summary>Noms de processus exclus de l'Optimiseur de performances (jamais proposés à la mise en veille).</summary>
    public List<string> ExcludedProcessNames { get; set; } = [];

    public bool StartWithWindows { get; set; }

    /// <summary>Identifiants Windows Update des pilotes que l'utilisateur a choisi d'ignorer.</summary>
    public List<string> IgnoredDriverUpdateIds { get; set; } = [];

    /// <summary>Si vrai, les fichiers supprimés par le Nettoyeur/Doublons sont déplacés en quarantaine au lieu d'être effacés définitivement (voir IQuarantineService).</summary>
    public bool QuarantineEnabled { get; set; } = true;

    /// <summary>Nombre de jours avant qu'une entrée de quarantaine ne soit purgée automatiquement.</summary>
    public int QuarantineRetentionDays { get; set; } = 7;
}
