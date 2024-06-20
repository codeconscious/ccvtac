using System.Text.RegularExpressions;
using static CCVTAC.FSharp.Settings;

namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Detection of specific text within video metadata files.
/// </summary>
internal static class Detectors
{
    /// <summary>
    /// Finds and returns the first instance of text matching a given detection scheme pattern,
    /// parsing into T if necessary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="videoMetadata"></param>
    /// <param name="patterns"></param>
    /// <param name="defaultValue">The value to return if nothing is matched.</param>
    /// <returns>A match of type T if there was a match; otherwise, the default value provided.</returns>
    internal static T? DetectSingle<T>(VideoMetadata videoMetadata,
                                       IEnumerable<TagDetectionPattern> patterns,
                                       T? defaultValue)
    {
        foreach (TagDetectionPattern pattern in patterns)
        {
            string searchText = ExtractMetadataText(videoMetadata, ConvertToSourceMetadataField(pattern.SearchField));
            Match match = new Regex(pattern.Regex).Match(searchText); // TODO: Instantiate when first reading settings.

            if (!match.Success)
                continue;

            string? matchedText = match.Groups[pattern.MatchGroup].Value.Trim();
            return Cast(matchedText, defaultValue);
        }

        return defaultValue; // No matches were found.
    }

    /// <summary>
    /// Finds and returns all instances of text matching a given detection scheme pattern,
    /// concatentating them into a single string, then casting to type T if necessary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="patterns"></param>
    /// <param name="defaultValue">The value to return if nothing is matched.</param>
    /// <param name="separator"></param>
    /// <returns>A match of type T if there were any matches; otherwise, the default value provided.</returns>
    internal static T? DetectMultiple<T>(VideoMetadata data,
                                         IEnumerable<TagDetectionPattern> patterns,
                                         T? defaultValue,
                                         string separator = "; ")
    {
        HashSet<string> matchedValues = [];

        foreach (TagDetectionPattern pattern in patterns)
        {
            string searchText = ExtractMetadataText(data, ConvertToSourceMetadataField(pattern.SearchField.ToLowerInvariant()));
            MatchCollection matches = new Regex(pattern.Regex).Matches(searchText); // TODO: Instantiate when first reading settings.

            foreach (var match in matches.Where(m => m.Success))
            {
                matchedValues.Add(match.Groups[pattern.MatchGroup].Value.Trim());
            }
        }

        if (matchedValues.Count == 0)
        {
            return defaultValue;
        }

        string joinedMatchedText = string.Join(separator, matchedValues);
        return Cast(joinedMatchedText, defaultValue);
    }

    /// <summary>
    /// Attempts casting the input text to type T and returning it.
    /// If casting fails, the default value is returned instead.
    /// </summary>
    private static T? Cast<T>(string? text, T? defaultValue)
    {
        if (text is T)
        {
            return (T)(object)text;
        }

        try
        {
            return (T?)Convert.ChangeType(text, typeof(T));
        }
        catch (InvalidCastException)
        {
            return defaultValue;
        }
    }

    private static SourceMetadataField ConvertToSourceMetadataField(string tagName)
    {
        return tagName switch
        {
            "title" => SourceMetadataField.Title,
            "description" => SourceMetadataField.Description,
            _ => throw new ArgumentException($"\"{tagName}\" is an invalid tag name.")
        };
    }

    /// <summary>
    /// Extracts the value of the specified tag field from the given data.
    /// </summary>
    private static string ExtractMetadataText(VideoMetadata videoMetadata,
                                              SourceMetadataField target)
    {
        return target switch
        {
            SourceMetadataField.Title       => videoMetadata.Title,
            SourceMetadataField.Description => videoMetadata.Description,
            _ => throw new ArgumentException($"\"{target}\" is an invalid {nameof(SourceMetadataField)}.")
        };
    }
}
