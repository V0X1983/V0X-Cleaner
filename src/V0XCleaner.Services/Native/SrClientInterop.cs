namespace V0XCleaner.Services.Native;

/// <summary>
/// P/Invoke vers srclient.dll : la classe WMI SystemRestore permet de lister et créer des points
/// de restauration, mais ne fournit aucune méthode pour en supprimer un seul. SRRemoveRestorePoint
/// est la fonction que la restauration système de Windows utilise elle-même pour ça.
/// </summary>
internal static partial class SrClientInterop
{
    [System.Runtime.InteropServices.LibraryImport("srclient.dll")]
    public static partial uint SRRemoveRestorePoint(uint index);
}
