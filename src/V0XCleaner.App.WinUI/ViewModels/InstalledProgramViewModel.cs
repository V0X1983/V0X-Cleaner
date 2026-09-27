using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using V0XCleaner.App.WinUI.Helpers;
using V0XCleaner.Core.Models;
using Windows.Storage.Streams;

namespace V0XCleaner.App.WinUI.ViewModels;

public partial class InstalledProgramViewModel(InstalledProgram program) : ObservableObject
{
    public InstalledProgram Program { get; } = program;

    public string DisplayName => Program.DisplayName;

    public string DisplayVersion => Program.DisplayVersion ?? "—";

    public string Publisher => Program.Publisher ?? "—";

    public string FormattedSize => Program.EstimatedSizeBytes is { } bytes ? ByteFormatter.Format(bytes) : "—";

    public string FormattedInstallDate => Program.InstallDate?.ToString("dd/MM/yyyy") ?? "—";

    public string KindLabel => Program.Kind == InstalledProgramKind.Win32 ? "Application" : "App Microsoft Store";

    public string DetailsLine => string.Join("  ·  ", new[]
    {
        Program.Publisher,
        Program.DisplayVersion is { } version ? "v" + version : null,
        Program.InstallDate?.ToString("dd/MM/yyyy"),
        KindLabel
    }.Where(part => !string.IsNullOrWhiteSpace(part)));

    public string Initial => DisplayName.Length > 0 ? DisplayName[..1].ToUpperInvariant() : "?";

    [ObservableProperty]
    public partial ImageSource? Icon { get; set; }

    /// <summary>
    /// Contrairement à WPF (Imaging.CreateBitmapSourceFromHIcon, direct depuis un HICON), WinUI 3 n'a
    /// pas d'équivalent direct pour convertir un System.Drawing.Icon en ImageSource : l'icône est donc
    /// réencodée en PNG en mémoire (System.Drawing, thread d'arrière-plan), puis chargée via
    /// BitmapImage.SetSourceAsync (doit s'exécuter sur le thread UI, d'où l'absence de Task.Run autour
    /// de cette dernière étape).
    /// </summary>
    public async Task LoadIconAsync()
    {
        if (Program.IconPath is not { } path)
        {
            return;
        }

        var pngBytes = await Task.Run(() =>
        {
            try
            {
                using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon is null)
                {
                    return null;
                }

                using var bitmap = icon.ToBitmap();
                using var ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch (Exception ex) when (ex is ArgumentException or Win32Exception or IOException or NotSupportedException)
            {
                return null;
            }
        });

        if (pngBytes is null)
        {
            return;
        }

        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(pngBytes.AsBuffer());
        stream.Seek(0);

        var bitmapImage = new BitmapImage();
        await bitmapImage.SetSourceAsync(stream);
        Icon = bitmapImage;
    }

    public bool CanUninstall => Program.CanUninstall;

    public bool IsWin32 => Program.Kind == InstalledProgramKind.Win32;

    public bool CanMove => IsWin32 && !string.IsNullOrWhiteSpace(Program.InstallLocation);

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public override string ToString() => DisplayName;
}
