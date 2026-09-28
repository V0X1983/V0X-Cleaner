namespace V0XCleaner.App.WinUI.ViewModels;

public enum ProgramSortMode
{
    NameAscending,
    NameDescending,
    SizeDescending,
    SizeAscending,
    PublisherAscending,
    InstallDateDescending
}

public sealed record ProgramSortOption(ProgramSortMode Mode, string Label)
{
    public override string ToString() => Label;
}
