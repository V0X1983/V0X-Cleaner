using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace V0XCleaner.App.WinUI.Helpers;

/// <summary>Masque un élément quand la valeur booléenne liée est vraie (ex. cacher le bouton d'élévation une fois élevé).</summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
