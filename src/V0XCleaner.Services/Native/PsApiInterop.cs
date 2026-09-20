using System.Runtime.InteropServices;

namespace V0XCleaner.Services.Native;

/// <summary>P/Invoke vers psapi.dll pour réduire le working set d'un processus ("libérer la RAM").</summary>
internal static partial class PsApiInterop
{
    [LibraryImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EmptyWorkingSet(nint hProcess);
}
