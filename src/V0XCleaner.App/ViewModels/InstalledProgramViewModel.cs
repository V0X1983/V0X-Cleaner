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

    public bool CanUninstall => Program.CanUninstall;

    public bool IsWin32 => Program.Kind == InstalledProgramKind.Win32;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public override string ToString() => DisplayName;
}
