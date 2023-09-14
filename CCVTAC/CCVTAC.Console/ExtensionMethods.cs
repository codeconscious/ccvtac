using System.IO;
using System.Text;

namespace CCVTAC.Console;

public static class ExtensionMethods
{
    /// <summary>
    /// Returns a new string in which all invalid path characters for the current OS
    /// have been replaced by specifed replacement character.
    /// Throws if the replacement character is an invalid path character.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="replaceWith"></param>
    /// <returns></returns>
    public static string ReplaceInvalidPathChars(
        this string text,
        char replaceWith = '_',
        char[]? customInvalidChars = null)
    {
        var invalidChars = Path.GetInvalidFileNameChars()
                               .Concat(Path.GetInvalidPathChars())
                               .Concat(new [] {
                                    Path.PathSeparator,
                                    Path.DirectorySeparatorChar,
                                    Path.AltDirectorySeparatorChar,
                                    Path.VolumeSeparatorChar })
                               .Concat(customInvalidChars ?? Enumerable.Empty<char>())
                               .Distinct();

        if (invalidChars.Contains(replaceWith))
            throw new ArgumentException($"The replacement char ('{replaceWith}') must be a valid path character.");

        return invalidChars.Aggregate(
            new StringBuilder(text),
            (workingText, ch) => workingText.Replace(ch, replaceWith),
            (workingText)     => workingText.ToString()
        );
    }
}
