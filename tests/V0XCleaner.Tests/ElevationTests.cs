using System.Text.Json;
using V0XCleaner.Core.Models.Elevation;
using V0XCleaner.Services.Elevation;

namespace V0XCleaner.Tests;

public class ElevationTests
{
    [Fact]
    public void ElevatedBatchRequest_JsonRoundTrip_PreservesOperations()
    {
        var request = new ElevatedBatchRequest(
        [
            new ElevatedRegistryOperation("item-1", ElevatedRegistryOperationKind.DeleteKey, "HKLM", @"Software\Test", "OrphanKey"),
            new ElevatedRegistryOperation("item-2", ElevatedRegistryOperationKind.DeleteValue, "HKCR", @"CLSID\{Broken}", "SomeValue"),
        ]);

        var json = JsonSerializer.Serialize(request);
        var roundTripped = JsonSerializer.Deserialize<ElevatedBatchRequest>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(2, roundTripped.Operations.Count);
        Assert.Equal("item-1", roundTripped.Operations[0].OperationId);
        Assert.Equal(ElevatedRegistryOperationKind.DeleteKey, roundTripped.Operations[0].Kind);
        Assert.Equal("HKLM", roundTripped.Operations[0].HivePrefix);
        Assert.Equal(@"Software\Test", roundTripped.Operations[0].Path);
        Assert.Equal("OrphanKey", roundTripped.Operations[0].Name);
        Assert.Equal(ElevatedRegistryOperationKind.DeleteValue, roundTripped.Operations[1].Kind);
    }

    [Fact]
    public void ElevatedBatchResponse_JsonRoundTrip_PreservesResults()
    {
        var response = new ElevatedBatchResponse(
            Success: true,
            ErrorMessage: null,
            Results:
            [
                new ElevatedOperationResult("item-1", true, null),
                new ElevatedOperationResult("item-2", false, "Accès refusé."),
            ]);

        var json = JsonSerializer.Serialize(response);
        var roundTripped = JsonSerializer.Deserialize<ElevatedBatchResponse>(json);

        Assert.NotNull(roundTripped);
        Assert.True(roundTripped.Success);
        Assert.Equal(2, roundTripped.Results.Count);
        Assert.False(roundTripped.Results[1].Success);
        Assert.Equal("Accès refusé.", roundTripped.Results[1].ErrorMessage);
    }

    [Fact]
    public async Task NullElevatedOperationClient_IsNotSupported_AndFailsWithoutLaunchingAnything()
    {
        var client = new NullElevatedOperationClient();

        Assert.False(client.IsSupported);

        var response = await client.ExecuteAsync([]);

        Assert.False(response.Success);
        Assert.Empty(response.Results);
    }

    [Fact]
    public void ElevatedHelperLauncher_FindsAndDeploysHelperFromSiblingBuildOutput()
    {
        // Dépend du fait que V0XCleaner.sln ait déjà été build (voir PROMPT.md, "dotnet build
        // V0XCleaner.sln" est le workflow standard avant "dotnet test" dans ce dépôt) : c'est ce
        // qui rend V0XCleaner.ElevatedHelper.exe trouvable sous src/V0XCleaner.ElevatedHelper/bin.
        var launcher = new ElevatedHelperLauncher();

        var deployed = launcher.TryEnsureDeployed();

        Assert.True(deployed, "V0XCleaner.ElevatedHelper.exe introuvable : lancer 'dotnet build V0XCleaner.sln' avant les tests.");
        Assert.NotNull(launcher.DeployedHelperExePath);
        Assert.True(File.Exists(launcher.DeployedHelperExePath));
    }
}
