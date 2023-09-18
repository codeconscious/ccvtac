using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// A scheme used to detect specific information (artist, title, year, etc.) within video metadata.
/// </summary>
/// <param name="Regex">Provides the search pattern.</param>
/// <param name="MatchGroup">
///     The regex match group number whose value should be returned. Zero represents the entire matched text,
///     and 1 and greater represent groups specified within the regex pattern.
/// </param>
/// <param name="TagName">The video metadata field to attempt searching in.</param>
/// <param name="Note">An optional note that can be used to summarize what the regex is searching for, etc.</param>
internal record struct DetectionScheme
{
    internal Regex       Regex;
    internal int         MatchGroup;
    internal SourceField SourceField;
    internal string?     Note;

    internal DetectionScheme(string      regexPattern,
                             int         matchGroup,
                             SourceField sourceField,
                             string?     note = null
    )
    {
        Regex = new Regex(regexPattern, RegexOptions.None);
        MatchGroup = matchGroup;
        SourceField = sourceField;
        Note = note;
    }
};
