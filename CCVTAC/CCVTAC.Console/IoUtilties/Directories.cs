using System.IO;

namespace CCVTAC.Console.IoUtilties;

internal static class Directories
{
    /// <summary>
    /// Counts the number of files in the specified directory.
    /// </summary>
    /// <param name="workingDirectory"></param>
    /// <param name="customIgnoreFiles">An optional list of files to be excluded.</param>
    internal static ImmutableList<string> GetDirectoryFiles(string workingDirectory,
                                                            IEnumerable<string>? customIgnoreFiles = null)
    {
        var ignoreFiles = customIgnoreFiles?.Distinct() ?? Enumerable.Empty<string>();

        return Directory.GetFiles(workingDirectory, "*", new EnumerationOptions())
                        .Where(dirFilePath => !ignoreFiles.Any(ignoreFile => dirFilePath.EndsWith(ignoreFile)))
                        .ToImmutableList();
    }
}
