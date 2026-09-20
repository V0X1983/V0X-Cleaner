using System.Runtime.InteropServices;

namespace V0XCleaner.Services.Native;

/// <summary>P/Invoke pour geler/réactiver un processus entier (mode veille de l'Optimiseur de performances).</summary>
internal static partial class NtDllInterop
{
    public const uint ProcessSuspendResume = 0x0800;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, int processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    [LibraryImport("ntdll.dll")]
    public static partial int NtSuspendProcess(nint processHandle);

    [LibraryImport("ntdll.dll")]
    public static partial int NtResumeProcess(nint processHandle);
}
