using CommunityToolkit.Mvvm.ComponentModel;

namespace V0XCleaner.App.ViewModels;

public partial class DuplicateFileEntryViewModel(string filePath) : ObservableObject
{
    public string FilePath { get; } = filePath;

    [ObservableProperty]
    private bool _isSelected;
}
