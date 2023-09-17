using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing.Tagging;

public static class Detectors
{
    private static string ExtractText(YouTubeVideoJson.Root videoMetadata, SourceTag target) =>
        target switch
        {
            SourceTag.Title       => videoMetadata.Title,
            SourceTag.Description => videoMetadata.Description,
            _ => throw new ArgumentException($"\"{target}\" is an invalid {nameof(SourceTag)}.")
        };

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
