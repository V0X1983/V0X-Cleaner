using V0XCleaner.Services.FileSystem;

namespace V0XCleaner.Tests;

public class ExclusionMatcherTests
{
    [Fact]
    public void FileInsideExcludedFolder_IsExcluded()
    {
        Assert.True(ExclusionMatcher.IsExcluded(@"C:\Data\Keep\a.tmp", [@"C:\Data\Keep"]));
        Assert.True(ExclusionMatcher.IsExcluded(@"C:\Data\Keep\a.tmp", [@"c:\data\keep\"]));
    }

    [Fact]
    public void ExactFile_IsExcluded()
    {
        Assert.True(ExclusionMatcher.IsExcluded(@"C:\Data\a.tmp", [@"C:\Data\a.tmp"]));
    }

    [Fact]
    public void SiblingWithSamePrefix_IsNotExcluded()
    {
        Assert.False(ExclusionMatcher.IsExcluded(@"C:\Data\Keeper\a.tmp", [@"C:\Data\Keep"]));
    }

    [Fact]
    public void BlankOrInvalidEntries_AreIgnored()
    {
        Assert.False(ExclusionMatcher.IsExcluded(@"C:\Data\a.tmp", ["", "  ", "\0bad"]));
    }
}
