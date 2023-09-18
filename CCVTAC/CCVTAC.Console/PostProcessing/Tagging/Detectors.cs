using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing.Tagging;

public static class Detectors
{
    /// <summary>
    /// Extracts the value of the specified tag field from the given data.
    /// </summary>
    private static string ExtractText(YouTubeVideoJson.Root videoMetadata, SourceTag target) =>
        target switch
        {
            SourceTag.Title       => videoMetadata.Title,
            SourceTag.Description => videoMetadata.Description,
            _ => throw new ArgumentException($"\"{target}\" is an invalid {nameof(SourceTag)}.")
        };

    /// <summary>
    /// Finds and returns the first instance of text matching a given detection scheme pattern,
    /// parsing into T if necessary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="videoMetadata"></param>
    /// <param name="schemes"></param>
    /// <param name="defaultValue"></param>
    /// <returns>A match of type T if there was a match; otherwise, the default value provided.</returns>
    public static T? DetectSingle<T>(
        YouTubeVideoJson.Root videoMetadata,
        IEnumerable<DetectionScheme> schemes,
        T? defaultValue)
    {
        foreach (var scheme in schemes)
        {
            var regex = new Regex(scheme.RegexPattern);
            var searchText = ExtractText(videoMetadata, scheme.TagName);
            var match = regex.Match(searchText);

            if (!match.Success)
                continue;

            string? output = match.Groups[scheme.MatchGroup].Value.Trim();

            if (output is T)
            {
                return (T)(object)output;
            }

            try
            {
                return (T?)Convert.ChangeType(output, typeof(T));
            }
            catch (InvalidCastException)
            {
                return defaultValue;
            }
        }

        return defaultValue;
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
    public static T? DetectMultiple<T>(
        YouTubeVideoJson.Root data,
        IEnumerable<DetectionScheme> schemes,
        T? defaultValue,
        string separator = "; ")
    {
        var matchedValues = new HashSet<string>();

        foreach (var scheme in schemes)
        {
            var regex = new Regex(scheme.RegexPattern);
            var searchText = ExtractText(data, scheme.TagName);
            var matches = regex.Matches(searchText);

            foreach (var match in matches.Where(m => m.Success))
            {
                matchedValues.Add(match.Groups[scheme.MatchGroup].Value.Trim());
            }
        }

        if (!matchedValues.Any())
        {
            return defaultValue;
        }

        var output = string.Join(separator, matchedValues);

        if (output is T)
        {
            return (T)(object)output;
        }

        try
        {
            return (T)Convert.ChangeType(output, typeof(T));
        }
        catch (InvalidCastException)
        {
            return defaultValue;
        }
    }
}
