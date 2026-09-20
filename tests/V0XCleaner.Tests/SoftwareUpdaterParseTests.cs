using V0XCleaner.Services;

namespace V0XCleaner.Tests;

public class SoftwareUpdaterParseTests
{
    [Fact]
    public void ParsesWingetTableWithSpinnerNoise()
    {
        var output = "   - \r   | \r\n" +
            "Nom             ID               Version   Disponible  Source\r\n" +
            "----------------------------------------------------------\r\n" +
            "Mozilla Firefox Mozilla.Firefox  130.0     131.0       winget\r\n" +
            "7-Zip           7zip.7zip        23.01     24.08       winget\r\n" +
            "2 mises à niveau disponibles.\r\n";

        var updates = SoftwareUpdater.Parse(output);

        Assert.Equal(2, updates.Count);
        Assert.Equal("7zip.7zip", updates[1].Id);
        Assert.Equal("24.08", updates[1].AvailableVersion);
    }

    [Fact]
    public void ReturnsEmptyWhenNoTable()
    {
        Assert.Empty(SoftwareUpdater.Parse("Aucune mise à niveau disponible."));
    }
}
