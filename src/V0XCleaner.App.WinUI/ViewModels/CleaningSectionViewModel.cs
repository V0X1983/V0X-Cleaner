namespace V0XCleaner.App.WinUI.ViewModels;

/// <summary>Regroupement affiché dans les pages Nettoyeur/Registre (ex. Système / Navigateurs / Applications tierces).</summary>
public sealed class CleaningSectionViewModel(string title, IReadOnlyList<CleaningTaskViewModel> tasks)
{
    public string Title { get; } = title;

    public IReadOnlyList<CleaningTaskViewModel> Tasks { get; } = tasks;
}
