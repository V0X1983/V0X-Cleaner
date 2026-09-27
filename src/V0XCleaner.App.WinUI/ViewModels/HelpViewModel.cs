using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using V0XCleaner.Services.Native;
using Windows.System;

namespace V0XCleaner.App.WinUI.ViewModels;

public partial class HelpViewModel : ObservableObject
{
    public string VersionText { get; } =
        "V0X Cleaner v" + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0");

    [RelayCommand]
    private Task OpenLogsFolderAsync() => OpenFolderAsync(AppPaths.LogsFolder);

    [RelayCommand]
    private Task OpenDataFolderAsync() => OpenFolderAsync(AppPaths.RootFolder);

    private static async Task OpenFolderAsync(string path)
    {
        try
        {
            await Launcher.LaunchFolderPathAsync(path);
        }
        catch (Exception ex) when (ex is FileNotFoundException or UnauthorizedAccessException)
        {
            // Dossier introuvable ou inaccessible : rien à faire.
        }
    }
}
