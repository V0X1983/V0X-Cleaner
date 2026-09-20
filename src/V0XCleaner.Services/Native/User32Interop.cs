using System.Runtime.InteropServices;

namespace V0XCleaner.Services.Native;

/// <summary>P/Invoke minimal vers user32.dll pour vider le presse-papiers.</summary>
internal static partial class User32Interop
{
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(nint hWndNewOwner);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll")]
    private static partial int CountClipboardFormats();

    public static bool HasClipboardContent() => CountClipboardFormats() > 0;

    public static bool TryEmptyClipboard()
    {
        if (!OpenClipboard(nint.Zero))
        {
            return false;
        }

        try
        {
            return EmptyClipboard();
        }
        finally
        {
            CloseClipboard();
        }
    }
}
