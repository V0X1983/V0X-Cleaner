using System.Globalization;

namespace V0XCleaner.App.WinUI.Helpers;

/// <summary>Formate une taille en octets en unité lisible (Ko, Mo, Go...), à la française.</summary>
public static class ByteFormatter
{
    private static readonly string[] Units = ["o", "Ko", "Mo", "Go", "To"];

    public static string Format(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 o";
        }

        double value = bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < Units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        var format = unitIndex == 0 ? "0" : "0.##";
        return $"{value.ToString(format, CultureInfo.InvariantCulture)} {Units[unitIndex]}";
    }
}
