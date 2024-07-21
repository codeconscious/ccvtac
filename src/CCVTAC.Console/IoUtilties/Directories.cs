using System.IO;

namespace CCVTAC.Console.IoUtilties;

internal static class Directories
{
    private static readonly string AllFilesSearchPattern = "*";
    private static readonly EnumerationOptions EnumerationOptions = new();

    /// <summary>
    /// Returns the filenames in a given directory, optionally ignoring specific filenames.
    /// </summary>
    /// <param name="directoryName"></param>
    /// <param name="customIgnoreFiles">An optional list of files to be excluded.</param>
    internal static ImmutableList<string> GetDirectoryFileNames(
        string directoryName,
        IEnumerable<string>? customIgnoreFiles = null)
    {
        var ignoreFiles = customIgnoreFiles?.Distinct() ?? [];

        return Directory
            .GetFiles(directoryName, AllFilesSearchPattern, EnumerationOptions)
            .Where(filePath =>
                ignoreFiles.None(ignoreFile => filePath.EndsWith(ignoreFile)))
            .ToImmutableList();
    }

    internal static Result WarnIfDirectoryHasFiles(string directory, Printer printer)
    {
        var fileNames = GetDirectoryFileNames(directory);

        if (fileNames.IsEmpty)
        {
            return Result.Ok();
        }

        var filesRemainLabel = fileNames.Count == 1 ? "file remains" : "files remain";
        var summary = $"{fileNames.Count} {filesRemainLabel} in the working directory:";
        printer.Error(summary);
        fileNames.ForEach(file => printer.Warning($"• {file}"));

        return Result.Fail(summary);
    }
}
