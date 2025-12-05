using System.IO;
using System.Text;

namespace CCVTAC.Console;

public static class ExtensionMethods
{
    /// <summary>
    /// Determines whether a string contains any text.
    /// </summary>
    /// <param name="maybeText"></param>
    /// <param name="allowWhiteSpace">Specifies whether whitespace characters should be considered as text.</param>
    /// <returns>Returns true if the string contains text; otherwise, false.</returns>
    public static bool HasText(this string? maybeText, bool allowWhiteSpace = false)
    {
        return allowWhiteSpace
            ? !string.IsNullOrEmpty(maybeText)
            : !string.IsNullOrWhiteSpace(maybeText);
    }

    extension<T>(IEnumerable<T> collection)
    {
        /// <summary>
        /// Determines whether a collection is empty.
        /// </summary>
        public bool None() => !collection.Any();

        /// <summary>
        /// Determines whether no elements of a sequence satisfy a given condition.
        /// </summary>
        public bool None(Func<T, bool> predicate) =>
            !collection.Any(predicate);
    }

    public static bool CaseInsensitiveContains(this IEnumerable<string> collection, string text) =>
        collection.Contains(text, new Comparers.CaseInsensitiveStringComparer());

    /// <param name="sourceText"></param>
    extension(string sourceText)
    {
        /// <summary>
        /// Returns a new string in which all invalid path characters for the current OS
        /// have been replaced by specified replacement character.
        /// Throws if the replacement character is an invalid path character.
        /// </summary>
        /// <param name="replaceWith"></param>
        /// <param name="customInvalidChars">Optional additional characters to consider invalid.</param>
        public string ReplaceInvalidPathChars(char replaceWith = '_',
            char[]? customInvalidChars = null
        )
        {
            var invalidChars = Path.GetInvalidFileNameChars()
                .Concat(Path.GetInvalidPathChars())
                .Concat(
                    [
                        Path.PathSeparator,
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar,
                        Path.VolumeSeparatorChar,
                    ]
                )
                .Concat(customInvalidChars ?? Enumerable.Empty<char>())
                .ToFrozenSet();

            if (invalidChars.Contains(replaceWith))
                throw new ArgumentException(
                    $"The replacement char ('{replaceWith}') must be a valid path character."
                );

            return invalidChars.Aggregate(
                new StringBuilder(sourceText),
                (workingText, ch) => workingText.Replace(ch, replaceWith),
                workingText => workingText.ToString()
            );
        }

        public string TrimTerminalLineBreak() =>
            sourceText.HasText() ? sourceText.TrimEnd(Environment.NewLine.ToCharArray()) : sourceText;
    }
}
