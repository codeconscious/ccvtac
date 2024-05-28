using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// A scheme used to detect specific information (artist, title, year, etc.) within video metadata.
/// </summary>
internal readonly record struct DetectionScheme
{
    /// <summary>
    /// The regular expression to be applied to the text of the SourceField.
    /// </summary>
    internal readonly Regex Regex;

    /// <summary>
    /// The number of the regular expression match group whose value should be used.
    /// (Matches can contain several groups. Group 0 represents the entire match.)
    /// </summary>
    internal readonly int MatchGroup;

    /// <summary>
    /// The video metadata field to which the regex should be applied (i.e., should search within).
    /// </summary>
    internal readonly SourceMetadataField SourceField;

    /// <summary>
    /// An optional memo about the match pattern.
    /// </summary>
    internal readonly string? Note;

    /// <summary>
    ///
    /// </summary>
    /// <param name="regexPattern">Provides the search pattern to be applied to the source field.</param>
    /// <param name="groupNumber">The regex match group number whose value should be returned.</param>
    /// <param name="sourceField">The video metadata field to attempt searching in.</param>
    /// <param name="note">An optional memo about the match pattern.</param>
    internal DetectionScheme(
        string regexPattern,
        MatchGroupId groupNumber,
        SourceMetadataField sourceField,
        string? note = null
    )
    {
        Regex = new Regex(regexPattern, RegexOptions.None);
        MatchGroup = (int) groupNumber;
        SourceField = sourceField;
        Note = note;
    }
};
