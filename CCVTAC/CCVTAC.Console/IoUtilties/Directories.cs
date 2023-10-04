using System.IO;

namespace CCVTAC.Console.IoUtilties;

internal static class Directories
{
    /// <summary>
    /// Counts the number of files in the specified directory.
    /// </summary>
    /// <param name="workingDirectory"></param>
    /// <param name="customIgnoreFiles">An optional list of files to be excluded.</param>
    internal static int DirectoryFileCount(
        string workingDirectory,
        IEnumerable<string>? customIgnoreFiles = null)
    {
        HashSet<string> ignoreFiles = new() { ".DS_Store" }; // Ignore macOS system files

        if (customIgnoreFiles?.Any() == true)
        {
            ignoreFiles.UnionWith(customIgnoreFiles);
        }

        return Directory.GetFiles(workingDirectory)
                        .Count(dirFile => !ignoreFiles.Any(ignoreFile => dirFile.EndsWith(ignoreFile)));
    }
}
