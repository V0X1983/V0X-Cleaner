namespace V0XCleaner.App.ViewModels;

/// <summary>Regroupement affiché dans la page Nettoyeur (Système / Navigateurs / Applications tierces).</summary>
public sealed class CleaningSectionViewModel(string title, IReadOnlyList<CleaningTaskViewModel> tasks)
{
    public string Title { get; } = title;

    public IReadOnlyList<CleaningTaskViewModel> Tasks { get; } = tasks;
}
