using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.WinUI.ViewModels;

public sealed class FolderSizeNodeViewModel(FolderSizeNode node, double barFraction)
{
    public FolderSizeNode Node { get; } = node;

    public string Name => Node.Name;

    public string FormattedSize => ByteFormatter.Format(Node.SizeBytes);

    public bool IsFile => Node.IsFile;

    /// <summary>Proportion (0 à 1) de la taille de l'élément le plus volumineux du niveau courant.</summary>
    public double BarFraction { get; } = barFraction;

    /// <summary>
    /// Largeur de la barre en pixels (200px max) : remplace le FractionToWidthConverter de WPF, WinUI
    /// n'ayant pas besoin d'un IValueConverter séparé pour un calcul aussi simple, à usage unique ici.
    /// </summary>
    public double BarWidth => Math.Max(2, BarFraction * 200);

    public override string ToString() => Name;
}
