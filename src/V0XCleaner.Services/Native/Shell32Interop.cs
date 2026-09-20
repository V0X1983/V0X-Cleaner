using System.Runtime.InteropServices;

namespace V0XCleaner.Services.Native;

/// <summary>P/Invoke minimal vers shell32.dll pour interroger et vider la Corbeille Windows.</summary>
internal static partial class Shell32Interop
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHQueryRecycleBinW(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SHEmptyRecycleBinW(nint hwnd, string? pszRootPath, uint dwFlags);

    private const uint SherbNoConfirmation = 0x00000001;
    private const uint SherbNoProgressUi = 0x00000002;
    private const uint SherbNoSound = 0x00000004;

    /// <summary>Taille totale actuelle de la Corbeille (tous lecteurs), en octets. 0 si vide ou indisponible.</summary>
    public static long GetRecycleBinSizeBytes()
    {
        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
        var hr = SHQueryRecycleBinW(null, ref info);
        return hr == 0 ? info.i64Size : 0;
    }

    /// <summary>Vide la Corbeille pour tous les lecteurs, sans confirmation ni UI (déjà géré par V0X Cleaner).</summary>
    public static bool EmptyRecycleBin()
    {
        var hr = SHEmptyRecycleBinW(nint.Zero, null, SherbNoConfirmation | SherbNoProgressUi | SherbNoSound);
        // S_OK (0) ou "corbeille déjà vide" (traité comme succès côté appelant)
        return hr == 0;
    }
}
