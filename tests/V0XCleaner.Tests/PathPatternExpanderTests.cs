using V0XCleaner.Services.FileSystem;

namespace V0XCleaner.Tests;

public class PathPatternExpanderTests : IDisposable
{
    private readonly string _root;

    public PathPatternExpanderTests()
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
    public void ExpandsLiteralPathToItself()
    {
        var target = Path.Combine(_root, "plain-folder");
        Directory.CreateDirectory(target);

        var results = PathPatternExpander.Expand(target).ToList();

        Assert.Single(results);
        Assert.Equal(target, results[0]);
    }

    [Fact]
    public void ExpandsMidSegmentWildcardAcrossMultipleProfiles()
    {
        var profile1Cache = Path.Combine(_root, "Profiles", "profile1.default", "cache2");
        var profile2Cache = Path.Combine(_root, "Profiles", "profile2.default", "cache2");
        Directory.CreateDirectory(profile1Cache);
        Directory.CreateDirectory(profile2Cache);
        Directory.CreateDirectory(Path.Combine(_root, "Profiles", "not-a-real-profile")); // no cache2 inside

        var pattern = Path.Combine(_root, "Profiles", "*", "cache2");
        var results = PathPatternExpander.Expand(pattern).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(profile1Cache, results);
        Assert.Contains(profile2Cache, results);
    }

    [Fact]
    public void ExpandsFileNameWildcardAtLeafSegment()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "thumbcache_001.db"), "x");
        File.WriteAllText(Path.Combine(_root, "thumbcache_002.db"), "x");
        File.WriteAllText(Path.Combine(_root, "unrelated.txt"), "x");

        var pattern = Path.Combine(_root, "thumbcache_*.db");
        var results = PathPatternExpander.Expand(pattern).ToList();

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.EndsWith(".db", r));
    }

    [Fact]
    public void ReturnsNothingWhenPathDoesNotExist()
    {
        var pattern = Path.Combine(_root, "does-not-exist", "*", "cache2");
        Assert.Empty(PathPatternExpander.Expand(pattern));
    }
}
