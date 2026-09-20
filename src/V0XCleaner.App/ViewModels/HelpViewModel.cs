using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using V0XCleaner.Services.Native;

namespace V0XCleaner.App.ViewModels;

public partial class HelpViewModel : ObservableObject
{
    public string VersionText { get; } =
        "V0X Cleaner v" + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0");

    [RelayCommand]
    private void OpenLogsFolder() => OpenFolder(AppPaths.LogsFolder);

    [RelayCommand]
    private void OpenDataFolder() => OpenFolder(AppPaths.RootFolder);

    private static void OpenFolder(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // Explorateur indisponible : rien à faire.
        }
    }
}
