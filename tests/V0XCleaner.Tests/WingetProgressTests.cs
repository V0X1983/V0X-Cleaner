using V0XCleaner.Services;

namespace V0XCleaner.Tests;

public class WingetProgressTests
{
    [Theory]
    [InlineData("Le code de hachage de l’installation a été vérifié avec succès")]
    [InlineData("Successfully verified installer hash")]
    [InlineData("Starting package install...")]
    public void RecognizesInstallLines(string line) => Assert.True(SoftwareUpdater.IsInstallLine(line));

    [Theory]
    [InlineData("Téléchargement en cours https://www.7-zip.org/a/7z2603-x64.msi")]
    [InlineData("Trouvé 7-Zip [7zip.7zip] Version 26.03")]
    public void DownloadOrInfoLinesAreNotInstallLines(string line) => Assert.False(SoftwareUpdater.IsInstallLine(line));

    [Fact]
    public void ExtractsDownloadUrl()
    {
        var found = WingetDownloadWatcher.TryGetUrl("Téléchargement en cours https://www.7-zip.org/a/7z2603-x64.msi", out var url);

        Assert.True(found);
        Assert.Equal("https://www.7-zip.org/a/7z2603-x64.msi", url);
    }

    [Fact]
    public void ReportsOnlyRecentFilesOfThePackage()
    {
        var root = Path.Combine(Path.GetTempPath(), "v0x-winget-test-" + Guid.NewGuid());
        try
        {
            var recentDir = Directory.CreateDirectory(Path.Combine(root, "Foo.Bar.2.0"));
            var otherDir = Directory.CreateDirectory(Path.Combine(root, "Other.Pkg.1.0"));
            var staleDir = Directory.CreateDirectory(Path.Combine(root, "Foo.Bar.1.0"));

            File.WriteAllBytes(Path.Combine(recentDir.FullName, "setup.exe"), new byte[1000]);
            File.WriteAllBytes(Path.Combine(otherDir.FullName, "big.exe"), new byte[5000]);
            var stale = Path.Combine(staleDir.FullName, "old.exe");
            File.WriteAllBytes(stale, new byte[9000]);
            File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddHours(-3));

            var size = WingetDownloadWatcher.LargestRecentFileSize(root, "Foo.Bar", DateTime.UtcNow.AddMinutes(-1));

            Assert.Equal(1000, size);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ReturnsZeroWhenTempFolderIsMissing() =>
        Assert.Equal(0, WingetDownloadWatcher.LargestRecentFileSize(Path.Combine(Path.GetTempPath(), "v0x-absent-" + Guid.NewGuid()), "Foo.Bar", DateTime.UtcNow));

    [Fact]
    public void ExplainsInstallLocationFailure()
    {
        var output = "Ce package nécessite un emplacement d’installation. Spécifier la racine d’installation : ";

        var message = SoftwareUpdater.ExplainFailure(output, -1978335166);

        Assert.Contains("Dossier d'installation requis", message);
    }

    [Theory]
    [InlineData("Programme d'installation :\n  Type du programme d’installation : burn\n  URL : https://x", "burn")]
    [InlineData("Installer:\n  Installer Type: MSI", "msi")]
    [InlineData("rien d'utile", null)]
    public void ParsesInstallerType(string output, string? expected) => Assert.Equal(expected, SoftwareUpdater.ParseInstallerType(output));

    [Fact]
    public void ExplainsUnknownFailureWithCode()
    {
        var message = SoftwareUpdater.ExplainFailure("erreur", -1978335166);

        Assert.Contains("0x8A150042", message);
    }
}
