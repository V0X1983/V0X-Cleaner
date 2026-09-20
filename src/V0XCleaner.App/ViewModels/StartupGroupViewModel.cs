namespace V0XCleaner.App.ViewModels;

public sealed class StartupGroupViewModel(string title, IReadOnlyList<StartupEntryViewModel> entries)
{
    public string Title { get; } = title;

    public IReadOnlyList<StartupEntryViewModel> Entries { get; } = entries;
}
