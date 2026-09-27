namespace V0XCleaner.App.WinUI.ViewModels;

/// <summary>Une entrée du menu latéral principal.</summary>
public sealed class NavigationItem
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Glyph { get; init; }

    public override string ToString() => Title;
}
