using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing.Tagging;

public static class Detectors
{
    public static T? DetectMultiple<T>(IEnumerable<DetectionScheme> schemes, T? defaultValue, string separator = "; ")
    {
        var matchedValues = new HashSet<string>();

        foreach (var scheme in schemes)
        {
            var regex = new Regex(scheme.Regex);
            var matches = regex.Matches(scheme.SourceText);

            foreach (var match in matches.Where(m => m.Success))
            {
                matchedValues.Add(match.Groups[scheme.Group].Value.Trim());
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

    public static T? DetectSingle<T>(IEnumerable<DetectionScheme> schemes, T? defaultValue)
    {
        foreach (var scheme in schemes)
        {
            var regex = new Regex(scheme.Regex);
            var match = regex.Match(scheme.SourceText);

            if (!match.Success)
                continue;

            string? output = match.Groups[scheme.Group].Value.Trim();

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
}
