using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace V0XCleaner.App.WinUI.Helpers;

/// <summary>Remplace WPF <c>MessageBox.Show</c> : WinUI n'a pas de MessageBox, un ContentDialog a besoin d'un XamlRoot.</summary>
public static class DialogHelper
{
    public static async Task<bool> ConfirmAsync(XamlRoot xamlRoot, string title, string message, string confirmText = "Confirmer", string cancelText = "Annuler")
    {
        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public static async Task NotifyAsync(XamlRoot xamlRoot, string title, string message, string closeText = "OK")
    {
        var dialog = new ContentDialog
        {
            XamlRoot = xamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = closeText
        };

        await dialog.ShowAsync();
    }
}
