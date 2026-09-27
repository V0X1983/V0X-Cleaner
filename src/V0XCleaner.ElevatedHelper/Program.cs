using System.Text.Json;
using Microsoft.Win32;
using V0XCleaner.Core.Models.Elevation;

namespace V0XCleaner.ElevatedHelper;

/// <summary>
/// Process compagnon full-trust, non empaqueté, lancé élevé (requireAdministrator, voir
/// app.manifest) par V0XCleaner.App.WinUI/App.WPF via ProcessElevatedOperationClient. Reçoit un
/// lot d'opérations registre via un fichier JSON d'entrée, les exécute, écrit le résultat dans un
/// fichier JSON de sortie, puis quitte immédiatement — ce n'est volontairement PAS un serveur
/// persistant (pas de pipe nommé à sécuriser contre UIPI, pas d'état à gérer entre deux appels).
///
/// Usage : V0XCleaner.ElevatedHelper.exe &lt;input.json&gt; &lt;output.json&gt;
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            return WriteFailureAndExit(args.ElementAtOrDefault(1), "Arguments invalides : chemins d'entrée/sortie manquants.");
        }

        var inputPath = args[0];
        var outputPath = args[1];

        try
        {
            var json = File.ReadAllText(inputPath);
            var request = JsonSerializer.Deserialize<ElevatedBatchRequest>(json);
            if (request is null)
            {
                return WriteFailureAndExit(outputPath, "Requête d'entrée illisible.");
            }

            var results = request.Operations.Select(ExecuteOperation).ToList();
            var response = new ElevatedBatchResponse(true, null, results);
            File.WriteAllText(outputPath, JsonSerializer.Serialize(response));
            return 0;
        }
        catch (Exception ex)
        {
            return WriteFailureAndExit(outputPath, $"Erreur inattendue du helper élevé : {ex.Message}");
        }
    }

    private static ElevatedOperationResult ExecuteOperation(ElevatedRegistryOperation operation)
    {
        try
        {
            using var baseKey = GetBaseKey(operation.HivePrefix);

            switch (operation.Kind)
            {
                case ElevatedRegistryOperationKind.DeleteKey:
                {
                    using var parentKey = operation.Path.Length == 0 ? baseKey : baseKey.OpenSubKey(operation.Path, writable: true);
                    parentKey?.DeleteSubKeyTree(operation.Name, throwOnMissingSubKey: false);
                    return new ElevatedOperationResult(operation.OperationId, true, null);
                }
                case ElevatedRegistryOperationKind.DeleteValue:
                {
                    using var subKey = baseKey.OpenSubKey(operation.Path, writable: true);
                    subKey?.DeleteValue(operation.Name, throwOnMissingValue: false);
                    return new ElevatedOperationResult(operation.OperationId, true, null);
                }
                default:
                    return new ElevatedOperationResult(operation.OperationId, false, "Opération inconnue.");
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or IOException)
        {
            return new ElevatedOperationResult(operation.OperationId, false, ex.Message);
        }
    }

    private static RegistryKey GetBaseKey(string hivePrefix) => hivePrefix switch
    {
        "HKCU" => Registry.CurrentUser,
        "HKLM" => Registry.LocalMachine,
        "HKCR" => Registry.ClassesRoot,
        "HKU" => Registry.Users,
        _ => throw new ArgumentOutOfRangeException(nameof(hivePrefix), hivePrefix, "Ruche de registre non prise en charge.")
    };

    private static int WriteFailureAndExit(string? outputPath, string message)
    {
        if (!string.IsNullOrEmpty(outputPath))
        {
            try
            {
                var response = new ElevatedBatchResponse(false, message, []);
                File.WriteAllText(outputPath, JsonSerializer.Serialize(response));
            }
            catch (IOException)
            {
                // Rien de plus à faire si même l'écriture du résultat d'échec échoue.
            }
        }

        return 1;
    }
}
