using System.IO;

namespace CCVTAC.Console.IoUtilties;

internal static class Directories
{
    private static readonly string AllFilesSearchPattern = "*";
    private static readonly EnumerationOptions EnumerationOptions = new();

    /// <summary>
    /// Counts the number of files in the specified directory.
    /// </summary>
    /// <param name="workingDirectory"></param>
    /// <param name="customIgnoreFiles">An optional list of files to be excluded.</param>
    internal static ImmutableList<string> GetDirectoryFiles(
        string workingDirectory,
        IEnumerable<string>? customIgnoreFiles = null)
    {
        var ignoreFiles = customIgnoreFiles?.Distinct() ?? [];

        return Directory.GetFiles(workingDirectory, AllFilesSearchPattern, EnumerationOptions)
                        .Where(dirFilePath =>
                            !ignoreFiles.Any(ignoreFile =>
                                dirFilePath.EndsWith(ignoreFile)))
                        .ToImmutableList();
    }

    internal static Result CheckIfEmpty(string directory)
    {
        var files = GetDirectoryFiles(directory);

        if (files.IsEmpty)
        {
            return Result.Ok();
        }

        var errors = files.Select(f => new Error(f));
        return Result.Fail(errors);
    }

    internal static Result WarnIfAnyFiles(string directory, Printer printer)
    {
        var result = CheckIfEmpty(directory);

        if (result.IsSuccess)
        {
            return result;
        }

        var files = result.Errors.Select(e => e.Message).ToImmutableList();
        var fileLabel = files.Count == 1 ? "file remains" : "files remain";
        var summary = $"{files.Count} {fileLabel} in the working directory:";

        printer.Error(summary);
        files.ForEach(file => printer.Warning($"• {file}"));

        return Result.Fail(summary);
    }
}
