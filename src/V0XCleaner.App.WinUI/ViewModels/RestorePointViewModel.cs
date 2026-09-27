using V0XCleaner.Core.Models;

namespace V0XCleaner.App.WinUI.ViewModels;

public sealed class RestorePointViewModel(RestorePointInfo info)
{
    public RestorePointInfo Info { get; } = info;

    public string Description => Info.Description;

    public string FormattedCreationTime => Info.CreationTime == DateTime.MinValue
        ? "—"
        : Info.CreationTime.ToString("dd/MM/yyyy HH:mm");

    public override string ToString() => Description;
}
