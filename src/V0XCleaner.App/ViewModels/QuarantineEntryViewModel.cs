using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public sealed class QuarantineEntryViewModel(QuarantineEntry entry)
{
    public QuarantineEntry Entry { get; } = entry;

    public string OriginalPath => Entry.OriginalPath;

    public string FormattedSize => ByteFormatter.Format(Entry.SizeBytes);

    public string FormattedDate => Entry.QuarantinedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}
