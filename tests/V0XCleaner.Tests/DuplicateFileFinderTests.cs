using V0XCleaner.Services;

namespace V0XCleaner.Tests;

public class DuplicateFileFinderTests : IDisposable
{
    private readonly string _root;
    private readonly DuplicateFileFinder _finder = new();

    public DuplicateFileFinderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "v0xcleaner-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task FindsIdenticalFilesAsDuplicates()
    {
        File.WriteAllText(Path.Combine(_root, "a.txt"), "same content");
        File.WriteAllText(Path.Combine(_root, "b.txt"), "same content");
        File.WriteAllText(Path.Combine(_root, "c.txt"), "different content!!");

        var groups = await _finder.FindDuplicatesAsync(_root);

        var group = Assert.Single(groups);
        Assert.Equal(2, group.FilePaths.Count);
    }

    [Fact]
    public async Task DoesNotFlagFilesWithSameSizeButDifferentContent()
    {
        File.WriteAllText(Path.Combine(_root, "a.txt"), "aaaa");
        File.WriteAllText(Path.Combine(_root, "b.txt"), "bbbb");

        var groups = await _finder.FindDuplicatesAsync(_root);

        Assert.Empty(groups);
    }

    [Fact]
    public async Task IgnoresEmptyFiles()
    {
        File.WriteAllText(Path.Combine(_root, "a.txt"), "");
        File.WriteAllText(Path.Combine(_root, "b.txt"), "");

        var groups = await _finder.FindDuplicatesAsync(_root);

        Assert.Empty(groups);
    }

    [Fact]
    public async Task GroupsThreeIdenticalFilesTogether()
    {
        File.WriteAllText(Path.Combine(_root, "a.txt"), "identical");
        File.WriteAllText(Path.Combine(_root, "b.txt"), "identical");
        File.WriteAllText(Path.Combine(_root, "c.txt"), "identical");

        var groups = await _finder.FindDuplicatesAsync(_root);

        var group = Assert.Single(groups);
        Assert.Equal(3, group.FilePaths.Count);
        Assert.Equal(group.SizeBytes * 2, group.WastedBytes);
    }
}
