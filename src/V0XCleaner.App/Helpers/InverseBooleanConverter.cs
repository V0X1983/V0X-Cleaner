using System.Globalization;
using System.Windows.Data;

namespace V0XCleaner.App.Helpers;

/// <summary>Inverse un booléen (ex: IsEnabled = !IsBusy).</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
}
