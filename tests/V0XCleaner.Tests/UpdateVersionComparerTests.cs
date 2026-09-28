using V0XCleaner.Core;

namespace V0XCleaner.Tests;

public class UpdateVersionComparerTests
{
    [Theory]
    [InlineData("0.3.0", "0.2.0", true)]
    [InlineData("0.2.0", "0.2.0", false)]
    [InlineData("0.1.0", "0.2.0", false)]
    [InlineData("1.0.0", "0.9.9", true)]
    public void ComparesAgainstInstalledVersion(string latest, string installed, bool expectedNewer)
    {
        Assert.Equal(expectedNewer, UpdateVersionComparer.IsNewer(latest, Version.Parse(installed)));
    }

    [Fact]
    public void ReturnsFalseWhenLatestVersionIsUnparseable()
    {
        Assert.False(UpdateVersionComparer.IsNewer("pas-une-version", Version.Parse("0.2.0")));
    }

    [Fact]
    public void ReturnsFalseWhenInstalledVersionIsNull()
    {
        Assert.False(UpdateVersionComparer.IsNewer("1.0.0", null));
    }
}
