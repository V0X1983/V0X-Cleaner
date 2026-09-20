using V0XCleaner.App.Helpers;
using V0XCleaner.Core.Models;

namespace V0XCleaner.App.ViewModels;

public sealed class DuplicateGroupViewModel(DuplicateFileGroup group, IReadOnlyList<DuplicateFileEntryViewModel> entries)
{
    public DuplicateFileGroup Group { get; } = group;

    public IReadOnlyList<DuplicateFileEntryViewModel> Entries { get; } = entries;

    public string FormattedSize => ByteFormatter.Format(Group.SizeBytes);

    public string FormattedWasted => ByteFormatter.Format(Group.WastedBytes);
}
