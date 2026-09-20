using V0XCleaner.Services.FileSystem;

namespace V0XCleaner.Tests;

public class PathGuardTests
{
    private readonly PathGuard _guard = new();

    [Theory]
    [InlineData(@"C:\")]
    [InlineData(@"D:\")]
    public void RejectsDriveRoots(string path)
    {
        Assert.False(_guard.IsSafeToDelete(path, out _));
    }

    [Fact]
    public void RejectsWindowsFolder()
    {
        var windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        Assert.False(_guard.IsSafeToDelete(windowsFolder, out _));
    }

    [Fact]
    public void RejectsProgramFilesFolder()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        Assert.False(_guard.IsSafeToDelete(programFiles, out _));
    }

    [Fact]
    public void RejectsUserProfileRoot()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Assert.False(_guard.IsSafeToDelete(userProfile, out _));
    }

    [Fact]
    public void AllowsOrdinaryTempFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "v0xcleaner-test-file.tmp");
        Assert.True(_guard.IsSafeToDelete(tempFile, out var reason));
        Assert.Null(reason);
    }

    [Fact]
    public void AllowsSubfolderOfWindowsFolder()
    {
        var windowsTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
        Assert.True(_guard.IsSafeToDelete(windowsTemp, out _));
    }

    [Fact]
    public void RejectsEmptyPath()
    {
        Assert.False(_guard.IsSafeToDelete("", out var reason));
        Assert.NotNull(reason);
    }
}
