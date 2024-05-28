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
    /// <param name="schemes"></param>
    /// <param name="defaultValue">The value to return if nothing is matched.</param>
    /// <returns>A match of type T if there was a match; otherwise, the default value provided.</returns>
    internal static T? DetectSingle<T>(VideoMetadata videoMetadata,
                                       IEnumerable<DetectionScheme> schemes,
                                       T? defaultValue)
    {
        foreach (DetectionScheme scheme in schemes)
        {
            string searchText = ExtractMetadataText(videoMetadata, scheme.SourceField);
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
    /// <param name="defaultValue">The value to return if nothing is matched.</param>
    /// <param name="separator"></param>
    /// <returns>A match of type T if there were any matches; otherwise, the default value provided.</returns>
    internal static T? DetectMultiple<T>(VideoMetadata data,
                                         IEnumerable<DetectionScheme> schemes,
                                         T? defaultValue,
                                         string separator = "; ")
    {
        HashSet<string> matchedValues = new();

        foreach (DetectionScheme scheme in schemes)
        {
            string searchText = ExtractMetadataText(data, scheme.SourceField);
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

        string joinedMatchedText = string.Join(separator, matchedValues);
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
