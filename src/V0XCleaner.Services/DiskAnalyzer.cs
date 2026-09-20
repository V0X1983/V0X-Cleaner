using V0XCleaner.Core.Abstractions;
using V0XCleaner.Core.Models;
using V0XCleaner.Services.FileSystem;

namespace V0XCleaner.Services;

/// <summary>Analyse un dossier niveau par niveau (comme un explorateur), sans construire un arbre complet en mémoire.</summary>
public sealed class DiskAnalyzer : IDiskAnalyzer
{
    public IReadOnlyList<string> GetAvailableDrives() =>
        DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.RootDirectory.FullName).ToList();

    public Task<IReadOnlyList<FolderSizeNode>> AnalyzeAsync(string path, CancellationToken cancellationToken = default)
    {
        var nodes = new List<FolderSizeNode>();

        IEnumerable<string> subDirectories = [];
        IEnumerable<string> files = [];

        try
        {
            subDirectories = Directory.EnumerateDirectories(path).ToList();
            files = Directory.EnumerateFiles(path).ToList();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return Task.FromResult<IReadOnlyList<FolderSizeNode>>(nodes);
        }

        foreach (var dir in subDirectories)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long size = 0;
            var count = 0;
            foreach (var file in FileSystemHelpers.EnumerateFilesSafe(dir))
            {
                cancellationToken.ThrowIfCancellationRequested();
                size += FileSystemHelpers.GetFileSizeSafe(file.FullName);
                count++;
            }

            nodes.Add(new FolderSizeNode
            {
                Name = Path.GetFileName(dir),
                FullPath = dir,
                SizeBytes = size,
                FileCount = count,
                IsFile = false
            });
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            nodes.Add(new FolderSizeNode
            {
                Name = Path.GetFileName(file),
                FullPath = file,
                SizeBytes = FileSystemHelpers.GetFileSizeSafe(file),
                FileCount = 1,
                IsFile = true
            });
        }

        return Task.FromResult<IReadOnlyList<FolderSizeNode>>(nodes.OrderByDescending(n => n.SizeBytes).ToList());
    }
}
