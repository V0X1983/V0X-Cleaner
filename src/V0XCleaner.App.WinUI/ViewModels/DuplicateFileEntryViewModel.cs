using CommunityToolkit.Mvvm.ComponentModel;

namespace V0XCleaner.App.WinUI.ViewModels;

public partial class DuplicateFileEntryViewModel(string filePath) : ObservableObject
{
    public string FilePath { get; } = filePath;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
