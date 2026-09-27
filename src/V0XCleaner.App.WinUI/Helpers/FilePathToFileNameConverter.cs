using Microsoft.UI.Xaml.Data;

namespace V0XCleaner.App.WinUI.Helpers;

/// <summary>Affiche uniquement le nom de fichier d'un chemin complet (ex: liste des sauvegardes registre).</summary>
public sealed class FilePathToFileNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is string path ? Path.GetFileName(path) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
