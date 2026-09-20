using V0XCleaner.Services.FileSystem;

namespace V0XCleaner.Tests;

public class FileSystemHelpersTests : IDisposable
{
    private readonly string _root;

    public FileSystemHelpersTests()
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
    public void DeleteDirectoryContents_RemovesFilesAndSubfoldersButKeepsRoot()
    {
        File.WriteAllText(Path.Combine(_root, "a.txt"), "a");
        var subDir = Path.Combine(_root, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "b.txt"), "b");

        var errors = FileSystemHelpers.DeleteDirectoryContents(_root);

        Assert.Empty(errors);
        Assert.True(Directory.Exists(_root));
        Assert.Empty(Directory.EnumerateFileSystemEntries(_root));
    }

    [Fact]
    public void GetDirectorySizeSafe_SumsFileSizes()
    {
        File.WriteAllBytes(Path.Combine(_root, "a.bin"), new byte[100]);
        File.WriteAllBytes(Path.Combine(_root, "b.bin"), new byte[50]);

        var size = FileSystemHelpers.GetDirectorySizeSafe(_root);

        Assert.Equal(150, size);
    }

    [Fact]
    public void TryDeleteFile_SucceedsWhenFileDoesNotExist()
    {
        var missing = Path.Combine(_root, "missing.txt");

        var result = FileSystemHelpers.TryDeleteFile(missing, out var error);

        Assert.True(result);
        Assert.Null(error);
    }
}
