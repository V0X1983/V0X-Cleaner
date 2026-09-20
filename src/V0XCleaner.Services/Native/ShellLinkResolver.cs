using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace V0XCleaner.Services.Native;

[ComImport]
[Guid("000214F9-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellLinkW
{
    // Seule la première méthode de la vtable réelle de IShellLinkW nous intéresse ; il est valide
    // de ne déclarer que celle-ci tant qu'elle reste bien la première méthode après IUnknown.
    void GetPath(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
        int cchMaxPath,
        IntPtr pfd,
        uint fFlags);
}

/// <summary>Résout la cible d'un raccourci Windows (.lnk) via l'objet COM ShellLink (CLSID_ShellLink).</summary>
internal static class ShellLinkResolver
{
    private static readonly Guid ShellLinkClsid = new("00021401-0000-0000-C000-000000000046");

    public static string? ResolveTarget(string lnkPath)
    {
        object? shellLinkObj = null;
        try
        {
            var shellLinkType = Type.GetTypeFromCLSID(ShellLinkClsid);
            if (shellLinkType is null)
            {
                return null;
            }

            shellLinkObj = Activator.CreateInstance(shellLinkType);
            if (shellLinkObj is not IPersistFile persistFile)
            {
                return null;
            }

            if (shellLinkObj is not IShellLinkW shellLink)
            {
                return null;
            }

            persistFile.Load(lnkPath, 0);

            var buffer = new StringBuilder(260);
            shellLink.GetPath(buffer, buffer.Capacity, IntPtr.Zero, 0);
            var target = buffer.ToString();
            return string.IsNullOrWhiteSpace(target) ? null : target;
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or FileNotFoundException or UnauthorizedAccessException)
        {
            return null;
        }
        finally
        {
            if (shellLinkObj is not null)
            {
                Marshal.ReleaseComObject(shellLinkObj);
            }
        }
    }
}
