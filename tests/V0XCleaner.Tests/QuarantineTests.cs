using Microsoft.Extensions.Logging.Abstractions;
using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services;
using V0XCleaner.Services.Cleaners;
using V0XCleaner.Services.FileSystem;

namespace V0XCleaner.Tests;

public class QuarantineTests : IDisposable
{
    private sealed class FakeSettings(bool quarantineEnabled) : ISettingsService
    {
        public AppSettings Current { get; } = new() { QuarantineEnabled = quarantineEnabled };
        public event EventHandler? SettingsChanged { add { } remove { } }
        public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private readonly string _root = Path.Combine(Path.GetTempPath(), "v0xcleaner-tests-" + Guid.NewGuid());
    private readonly QuarantineService _quarantine;

    public QuarantineTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "files"));
        _quarantine = new QuarantineService(NullLogger<QuarantineService>.Instance, Path.Combine(_root, "q"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string NewFile(string name = "a.tmp", string content = "hello")
    {
        var path = Path.Combine(_root, "files", name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Quarantine_MovesFileAndListsEntry()
    {
        var path = NewFile();

        var entry = _quarantine.Quarantine(path);

        Assert.NotNull(entry);
        Assert.False(File.Exists(path));
        Assert.True(File.Exists(entry!.QuarantinedPath));
        Assert.Single(_quarantine.GetEntries());
    }

    [Fact]
    public void Quarantine_MissingFile_ReturnsNull()
    {
        Assert.Null(_quarantine.Quarantine(Path.Combine(_root, "files", "nope.tmp")));
    }

    [Fact]
    public void Restore_PutsFileBack()
    {
        var path = NewFile(content: "data");
        var entry = _quarantine.Quarantine(path)!;

        Assert.True(_quarantine.Restore(entry.Id, out var error));
        Assert.Null(error);
        Assert.Equal("data", File.ReadAllText(path));
        Assert.Empty(_quarantine.GetEntries());
    }

    [Fact]
    public void Restore_RefusesToOverwriteExistingFile()
    {
        var path = NewFile();
        var entry = _quarantine.Quarantine(path)!;
        File.WriteAllText(path, "new");

        Assert.False(_quarantine.Restore(entry.Id, out var error));
        Assert.NotNull(error);
        Assert.Equal("new", File.ReadAllText(path));
        Assert.Single(_quarantine.GetEntries());
    }

    [Fact]
    public void PurgeExpired_RemovesOnlyOldEntries()
    {
        var entry = _quarantine.Quarantine(NewFile())!;

        Assert.Equal(0, _quarantine.PurgeExpired(TimeSpan.FromDays(1)));
        Assert.Equal(1, _quarantine.PurgeExpired(TimeSpan.Zero));
        Assert.False(File.Exists(entry.QuarantinedPath));
        Assert.Empty(_quarantine.GetEntries());
    }

    [Fact]
    public void PurgeEntry_DeletesPermanently()
    {
        var entry = _quarantine.Quarantine(NewFile())!;

        Assert.True(_quarantine.PurgeEntry(entry.Id));
        Assert.False(File.Exists(entry.QuarantinedPath));
    }

    private static CleanupItem ItemFor(string path) => new()
    {
        Id = path,
        DisplayPath = path,
        Category = CleanupCategory.SystemTemp,
        SizeBytes = 5
    };

    private FileDeletionCleaner NewCleaner(bool quarantineEnabled) =>
        new(new PathGuard(), _quarantine, new FakeSettings(quarantineEnabled), NullLogger<FileDeletionCleaner>.Instance);

    [Fact]
    public async Task Cleaner_Execute_QuarantinesWhenEnabled()
    {
        var path = NewFile();

        var result = await NewCleaner(true).CleanAsync([ItemFor(path)], OperationMode.Execute);

        Assert.Equal(1, result.SucceededCount);
        Assert.False(File.Exists(path));
        Assert.Single(_quarantine.GetEntries());
    }

    [Fact]
    public async Task Cleaner_Execute_DeletesPermanentlyWhenDisabled()
    {
        var path = NewFile();

        var result = await NewCleaner(false).CleanAsync([ItemFor(path)], OperationMode.Execute);

        Assert.Equal(1, result.SucceededCount);
        Assert.False(File.Exists(path));
        Assert.Empty(_quarantine.GetEntries());
    }

    [Fact]
    public async Task Cleaner_Simulate_TouchesNothing()
    {
        var path = NewFile();

        var result = await NewCleaner(true).CleanAsync([ItemFor(path)], OperationMode.Simulate);

        Assert.Equal(1, result.SucceededCount);
        Assert.True(File.Exists(path));
        Assert.Empty(_quarantine.GetEntries());
    }

    [Fact]
    public async Task Cleaner_LockedFile_ReportsErrorWithoutThrowing()
    {
        var path = NewFile();
        using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

        var result = await NewCleaner(true).CleanAsync([ItemFor(path)], OperationMode.Execute);

        Assert.Equal(1, result.FailedCount);
        Assert.True(File.Exists(path));
    }
}
