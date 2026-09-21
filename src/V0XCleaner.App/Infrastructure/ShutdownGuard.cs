using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace V0XCleaner.App.Infrastructure;

/// <summary>
/// Empêche un redémarrage lancé par un installateur pendant les mises à jour de logiciels : tant qu'une garde est active,
/// l'application refuse la fermeture de session et déclare un motif à Windows, qui affiche alors « cette application
/// empêche le redémarrage » avec le choix d'annuler. Un redémarrage forcé par l'installateur ne peut pas être bloqué.
/// </summary>
public static class ShutdownGuard
{
    private static int _depth;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShutdownBlockReasonCreate(nint hWnd, string reason);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShutdownBlockReasonDestroy(nint hWnd);

    /// <summary>Vrai tant qu'au moins une mise à jour est en cours.</summary>
    public static bool IsActive => Volatile.Read(ref _depth) > 0;

    public static IDisposable Begin(string reason)
    {
        var hwnd = GetMainWindowHandle();
        if (Interlocked.Increment(ref _depth) == 1 && hwnd != 0)
        {
            ShutdownBlockReasonCreate(hwnd, reason);
        }

        return new Releaser(hwnd);
    }

    private static nint GetMainWindowHandle()
    {
        var window = Application.Current?.MainWindow;
        return window is null ? 0 : new WindowInteropHelper(window).Handle;
    }

    private sealed class Releaser(nint hwnd) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0 && Interlocked.Decrement(ref _depth) == 0 && hwnd != 0)
            {
                ShutdownBlockReasonDestroy(hwnd);
            }
        }
    }
}
