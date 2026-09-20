using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public sealed class FolderSizeNodeViewModel(FolderSizeNode node, double barFraction)
{
    public FolderSizeNode Node { get; } = node;

    public string Name => Node.Name;

    public string FormattedSize => ByteFormatter.Format(Node.SizeBytes);

    public bool IsFile => Node.IsFile;

    /// <summary>Proportion (0 à 1) de la taille de l'élément le plus volumineux du niveau courant.</summary>
    public double BarFraction { get; } = barFraction;

    public override string ToString() => Name;
}
