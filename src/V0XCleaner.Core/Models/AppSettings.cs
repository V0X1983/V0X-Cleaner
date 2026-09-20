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

    public string UpdateCheckOwner { get; set; } = string.Empty;
    public string UpdateCheckRepo { get; set; } = string.Empty;

    public string Theme { get; set; } = "Dark";
}
