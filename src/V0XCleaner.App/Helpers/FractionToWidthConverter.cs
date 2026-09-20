using System.Globalization;
using System.Windows.Data;

namespace V0XCleaner.App.Helpers;

/// <summary>Convertit une fraction (0..1) en largeur de pixels, pour dessiner une barre proportionnelle simple.</summary>
public sealed class FractionToWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var fraction = value is double d ? d : 0;
        var maxWidth = parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var m) ? m : 200;
        return Math.Max(2, fraction * maxWidth);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
