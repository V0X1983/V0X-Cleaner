using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace V0XCleaner.App.Helpers;

/// <summary>Affiche uniquement le nom de fichier d'un chemin complet (ex: liste des sauvegardes registre).</summary>
public sealed class FilePathToFileNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string path ? Path.GetFileName(path) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
