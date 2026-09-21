using CommunityToolkit.Mvvm.ComponentModel;
using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

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
    private System.Windows.Media.ImageSource? _icon;

    public async Task LoadIconAsync()
    {
        if (Program.IconPath is not { } path)
        {
            return;
        }

        Icon = await Task.Run(() =>
        {
            try
            {
                using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon is null)
                {
                    return null;
                }

                var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return (System.Windows.Media.ImageSource)source;
            }
            catch (Exception ex) when (ex is ArgumentException or System.ComponentModel.Win32Exception or System.IO.IOException or NotSupportedException)
            {
                return null;
            }
        });
    }

    public bool CanUninstall => Program.CanUninstall;

    public bool IsWin32 => Program.Kind == InstalledProgramKind.Win32;

    public bool CanMove => IsWin32 && !string.IsNullOrWhiteSpace(Program.InstallLocation);

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public override string ToString() => DisplayName;
}
