using Microsoft.Win32;
using V0XCleaner.Services.RegistryCleanup;

namespace V0XCleaner.Tests;

public class RegistryPathHelperTests
{
    [Theory]
    [InlineData(@"HKCU\Software\Test\Key", "HKCU", @"Software\Test\Key")]
    [InlineData(@"HKLM\Software\Microsoft\Windows", "HKLM", @"Software\Microsoft\Windows")]
    public void SplitKeyId_SeparatesHiveFromSubPath(string id, string expectedHive, string expectedSubPath)
    {
        var (hive, subPath) = RegistryPathHelper.SplitKeyId(id);

        Assert.Equal(expectedHive, hive);
        Assert.Equal(expectedSubPath, subPath);
    }

    [Fact]
    public void SplitKeyId_WithNoBackslash_TreatsWholeStringAsHive()
    {
        var (hive, subPath) = RegistryPathHelper.SplitKeyId("HKCU");

        Assert.Equal("HKCU", hive);
        Assert.Equal(string.Empty, subPath);
    }

    [Theory]
    [InlineData(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MyApp", @"Software\Microsoft\Windows\CurrentVersion\Uninstall", "MyApp")]
    [InlineData("SingleSegment", "", "SingleSegment")]
    public void SplitParent_ExtractsParentAndLeaf(string subKeyPath, string expectedParent, string expectedLeaf)
    {
        var (parent, leaf) = RegistryPathHelper.SplitParent(subKeyPath);

        Assert.Equal(expectedParent, parent);
        Assert.Equal(expectedLeaf, leaf);
    }

    [Theory]
    [InlineData("HKCU")]
    [InlineData("HKLM")]
    [InlineData("HKCR")]
    [InlineData("HKU")]
    public void GetBaseKey_And_GetHivePrefix_RoundTrip(string prefix)
    {
        var baseKey = RegistryPathHelper.GetBaseKey(prefix);
        var roundTripped = RegistryPathHelper.GetHivePrefix(baseKey);

        Assert.Equal(prefix, roundTripped);
    }

    [Fact]
    public void GetBaseKey_WithUnknownPrefix_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RegistryPathHelper.GetBaseKey("NOPE"));
    }
}
