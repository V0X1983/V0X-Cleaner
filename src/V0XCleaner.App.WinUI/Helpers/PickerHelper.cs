using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace V0XCleaner.App.WinUI.Helpers;

/// <summary>
/// Sélecteurs de dossier/fichier WinUI 3 (<c>Windows.Storage.Pickers</c>) : contrairement à WPF
/// (<c>Microsoft.Win32.OpenFolderDialog</c>/<c>SaveFileDialog</c>, autonomes), ces API WinRT exigent
/// une association explicite à un HWND (<c>InitializeWithWindow</c>) avant affichage — sans quoi
/// <c>PickSingleFolderAsync</c>/<c>PickSaveFileAsync</c> lève une <see cref="System.Runtime.InteropServices.COMException"/>.
/// </summary>
public static class PickerHelper
{
    public static async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };
        picker.FileTypeFilter.Add("*");

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    public static async Task<string?> PickSaveFileAsync(string suggestedFileName, string fileTypeDescription, string extension)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = suggestedFileName
        };
        picker.FileTypeChoices.Add(fileTypeDescription, [extension]);

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));

        var file = await picker.PickSaveFileAsync();
        return file?.Path;
    }
}
