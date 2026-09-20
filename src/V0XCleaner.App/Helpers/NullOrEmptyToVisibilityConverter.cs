using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace V0XCleaner.App.Helpers;

/// <summary>Collapse un élément quand la valeur liée est null ou une chaîne vide (ex: message d'erreur optionnel).</summary>
public sealed class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
