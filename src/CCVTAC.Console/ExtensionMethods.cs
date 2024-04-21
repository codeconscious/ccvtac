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
    /// <param name="sourceText"></param>
    /// <param name="replaceWith"></param>
    /// <param name="customInvalidChars">Optional additional characters to consider invalid.</param>
    /// <returns></returns>
    public static string ReplaceInvalidPathChars(this string sourceText,
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
                               .ToFrozenSet();

        if (invalidChars.Contains(replaceWith))
            throw new ArgumentException($"The replacement char ('{replaceWith}') must be a valid path character.");

        return invalidChars.Aggregate(
            new StringBuilder(sourceText),
            (workingText, ch) => workingText.Replace(ch, replaceWith),
            (workingText)     => workingText.ToString()
        );
    }

    /// <summary>
    /// Determines whether a string is populated, containing any text.
    /// </summary>
    /// <param name="maybeText"></param>
    /// <param name="allowWhiteSpace">Specifies whether whitespace can be counted as characters.</param>
    /// <returns>true if the string contains text; otherwise, false.</returns>
    public static bool HasText(this string? maybeText, bool allowWhiteSpace = false)
    {
        return allowWhiteSpace
            ? !string.IsNullOrEmpty(maybeText)
            : !string.IsNullOrWhiteSpace(maybeText);
    }

    /// <summary>
    /// Determines whether a sequence contains no elements and, thus, is empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <returns>true if the source sequence contains no elements; otherwise, false.</returns>
    public static bool IsEmpty<T>(this IEnumerable<T> collection) => !collection.Any();
}
