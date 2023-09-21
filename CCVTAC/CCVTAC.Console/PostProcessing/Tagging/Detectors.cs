using System.Text.RegularExpressions;

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
    /// <returns>A match of type T if there was a match; otherwise, the default value provided.</returns>
    internal static T? DetectSingle<T>(YouTubeVideoJson.Root        videoMetadata,
                                       IEnumerable<DetectionScheme> schemes,
                                       T?                           defaultValue)
    {
        foreach (var scheme in schemes)
        {
            var searchText = ExtractMetadataText(videoMetadata, scheme.SourceField);
            var match = scheme.Regex.Match(searchText);

            if (!match.Success)
                continue;

            string? matchedText = match.Groups[scheme.MatchGroup].Value.Trim();
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
    /// <param name="schemes"></param>
    /// <param name="defaultValue"></param>
    /// <param name="separator"></param>
    /// <returns>A match of type T if there were any matches; otherwise, the default value provided.</returns>
    internal static T? DetectMultiple<T>(YouTubeVideoJson.Root        data,
                                         IEnumerable<DetectionScheme> schemes,
                                         T?                           defaultValue,
                                         string                       separator = "; ")
    {
        var matchedValues = new HashSet<string>();

        foreach (var scheme in schemes)
        {
            var searchText = ExtractMetadataText(data, scheme.SourceField);
            var matches = scheme.Regex.Matches(searchText);

            foreach (var match in matches.Where(m => m.Success))
            {
                matchedValues.Add(match.Groups[scheme.MatchGroup].Value.Trim());
            }
        }

        if (!matchedValues.Any())
        {
            return defaultValue;
        }

        var joinedMatchedText = string.Join(separator, matchedValues);
        return Cast(joinedMatchedText, defaultValue);
    }

    /// <summary>
    /// Attempts casting the input text to type T and returning it. If casting fails, the provided default value is returned instead.
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

    /// <summary>
    /// Extracts the value of the specified tag field from the given data.
    /// </summary>
    private static string ExtractMetadataText(
        YouTubeVideoJson.Root videoMetadata,
        SourceField target)
    {
        return target switch
        {
            SourceField.Title       => videoMetadata.Title,
            SourceField.Description => videoMetadata.Description,
            _                       => throw new ArgumentException($"\"{target}\" is an invalid {nameof(SourceField)}.")
        };
    }
}
