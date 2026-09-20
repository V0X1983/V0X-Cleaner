using System.Windows;
using System.Windows.Media;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public sealed class HealthCheckItemViewModel(HealthCheckItem item)
{
    public HealthCheckItem Item { get; } = item;

    public string Title => Item.Title;

    public string Description => Item.Description;

    public string Recommendation => Item.Recommendation;

    public int Score => Item.Score;

    public string SeverityLabel => Item.Severity switch
    {
        HealthCheckSeverity.Good => "Bon",
        HealthCheckSeverity.Warning => "À surveiller",
        HealthCheckSeverity.Critical => "Critique",
        _ => string.Empty
    };

    public Brush SeverityBrush
    {
        get
        {
            var key = Item.Severity switch
            {
                HealthCheckSeverity.Good => "BrushAccent",
                HealthCheckSeverity.Warning => "BrushWarning",
                _ => "BrushDanger"
            };
            return (Brush)Application.Current.Resources[key];
        }
    }
}
