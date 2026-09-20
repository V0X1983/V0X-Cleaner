namespace V0XCleaner.App.ViewModels;

public enum ProgramSortMode
{
    NameAscending,
    NameDescending,
    SizeDescending,
    PublisherAscending,
    InstallDateDescending
}

public sealed record ProgramSortOption(ProgramSortMode Mode, string Label)
{
    public override string ToString() => Label;
}
