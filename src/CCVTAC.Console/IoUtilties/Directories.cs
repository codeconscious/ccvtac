using System.IO;
using System.Text;

namespace CCVTAC.Console.IoUtilties;

internal static class Directories
{
    private static readonly string AllFilesSearchPattern = "*";
    private static readonly EnumerationOptions EnumerationOptions = new();

    internal static Result WarnIfHasFiles(string directory, int showMax)
    {
        var fileNames = GetDirectoryFileNames(directory);
        int fileCount = fileNames.Length;

        if (fileNames.IsEmpty)
        {
            return Result.Ok();
        }

        var remainLabel = fileCount == 1 ? "file remains" : "files remain";
        var report = new StringBuilder($"{fileCount} {remainLabel} in working directory \"{directory}\":");

        foreach (string fileName in fileNames.Take(showMax))
        {
            report.AppendLine($"• {fileName}");
        }

        if (fileCount > showMax)
        {
            report.AppendLine($"... plus {fileCount - showMax} more.");
        }

        return Result.Fail(report.ToString());
    }

    /// <summary>
    /// Returns the filenames in a given directory, optionally ignoring specific filenames.
    /// </summary>
    /// <param name="directoryName"></param>
    /// <param name="customIgnoreFiles">An optional list of files to be excluded.</param>
    private static ImmutableArray<string> GetDirectoryFileNames(
        string directoryName,
        IEnumerable<string>? customIgnoreFiles = null)
    {
        var ignoreFiles = customIgnoreFiles?.Distinct() ?? [];

        return
            [
                ..Directory
                    .GetFiles(directoryName, AllFilesSearchPattern, EnumerationOptions)
                    .Where(filePath =>
                        ignoreFiles.None(ignoreFile => filePath.EndsWith(ignoreFile)))
            ];
    }
}
