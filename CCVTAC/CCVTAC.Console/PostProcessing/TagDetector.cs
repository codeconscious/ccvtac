using System.Text;
using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing;

/// <summary>
/// A scheme used to apply regex to search specific text to search for matches,
/// then specify a regex match group
/// </summary>
/// <param name="Regex">A regex pattern that will be used to instantiate a new `Regex`.</param>
/// <param name="Group">
///     The regex match group whose value should be used.
///     Use `(` and `)` in the pattern to make groups.
///     Zero represents the entire match.
/// </param>
/// <param name="SearchText">The text to which the regex pattern should be applied.</param>
/// <param name="Source">The source of the match, used only for user output.</param>
public record struct DetectionScheme(
    string Regex, // TODO: Might be worth instantiating the `Regex` instance in the ctor.
    int    Group,
    string SourceText,
    string Source
);

internal class TagDetector
{
    public string? DetectTitle(YouTubeVideoJson.Root data, Printer printer, string? defaultName = null)
    {
        // TODO: Put this somewhere where it can be static.
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
                1,
                data.Description,
                "description (Topic style)"
            ),
            new DetectionScheme(
                @"(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]",
                2,
                data.Title,
                "title"
            ),
        };

        foreach (var pattern in parsePatterns)
        {
            var regex = new Regex(pattern.Regex);
            var match = regex.Match(pattern.SourceText);

            if (!match.Success)
                continue;

            printer.Print($"• Writing title \"{match.Groups[pattern.Group].Value}\" (matched via {pattern.Source})");
            return match.Groups[pattern.Group].Value.Trim();
        }

        printer.Print($"• No title was parsed{(defaultName is null ? "." : $", so defaulting to \"{defaultName}\".")}");
        return defaultName;
    }

    public string? DetectArtist(YouTubeVideoJson.Root data, Printer printer, string? defaultName = null)
    {
        // TODO: Put this somewhere where it can be static.
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
                2,
                data.Description,
                "description (Topic style)"
            ),
            new DetectionScheme(
                @"(.+)(?: - )?[「『](.+)[」』]\[([12]\d{3})\]",
                1,
                data.Title,
                "title"
            ),
        };

        foreach (var pattern in parsePatterns)
        {
            var regex = new Regex(pattern.Regex);
            var match = regex.Match(pattern.SourceText);

            if (!match.Success)
                continue;

            printer.Print($"• Writing artist \"{match.Groups[pattern.Group].Value}\" (matched via {pattern.Source})");
            return match.Groups[pattern.Group].Value.Trim();
        }

        printer.Print($"• No artist was parsed{(defaultName is null ? "." : $", so defaulting to \"{defaultName}\".")}");
        return defaultName;
    }

    public string? DetectAlbum(YouTubeVideoJson.Root data, Printer printer, string? defaultName = null)
    {
        // TODO: Put this somewhere where it can be static or else a setting.
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(?<=[Aa]lbum: ).+",
                0,
                data.Description,
                "description"
            ),
            new DetectionScheme(
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
                3,
                data.Description,
                "description (Topic style)"
            ),
            new DetectionScheme(
                """(?<='s ['"]).+(?=['"] album)""",
                0,
                data.Description,
                "description"
            ),
            new DetectionScheme(
                """(?<=Vol\.\d『).+(?=』\s?#\d)""",
                0,
                data.Description,
                "description"
            ),
            new DetectionScheme(
                """(?<=^\w{3}アルバム『).+(?=』)""",
                0,
                data.Description,
                ""
            ),
        };

        foreach (var pattern in parsePatterns)
        {
            var regex = new Regex(pattern.Regex);
            var match = regex.Match(pattern.SourceText);

            if (!match.Success)
                continue;

            printer.Print($"• Writing album \"{match.Groups[pattern.Group].Value}\" (matched via {pattern.Source})");
            return match.Groups[pattern.Group].Value.Trim();
        }

        printer.Print($"• No album was parsed{(defaultName is null ? "." : $", so defaulting to \"{defaultName}\".")}");
        return defaultName;
    }

    public string? DetectComposers(YouTubeVideoJson.Root data, Printer printer)
    {
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+",
                0,
                data.Description,
                "description"
            ),
            new DetectionScheme(
                @"(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+",
                0,
                data.Title,
                "title"
            )
        };

        var composers = DetectMultiple<string>(parsePatterns, null);

        if (composers?.Any() == true)
        {
            var composerText = string.Join("; ", composers);
            printer.Print($"• Writing composer(s) \"{composerText}\"");
            return composerText;
        }
        else
        {
            printer.Print($"• No composers parsed.");
            return null;
        }
    }

    public T? DetectMultiple<T>(IEnumerable<DetectionScheme> parsePatterns, T? defaultValue, string separator = "; ")
    {
        var matchedValues = new HashSet<string>();

        foreach (var pattern in parsePatterns)
        {
            var regex = new Regex(pattern.Regex);
            var matches = regex.Matches(pattern.SourceText);

            foreach (var match in matches.Where(m => m.Success))
            {
                matchedValues.Add(match.Groups[pattern.Group].Value.Trim());
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

    /// <summary>
    /// Attempt to automatically detect a release year in the video metadata.
    /// If none is found, return a default value.
    /// </summary>
    public ushort? DetectReleaseYear(YouTubeVideoJson.Root data, Printer printer, ushort? defaultYear = null)
    {
        // TODO: Feature: Use the video upload date for uploaders specified in user settings.
        // TODO: Put this somewhere where it can be static or made a setting.
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(?<=[(（\[［【])[12]\d{3}(?=[)）\]］】])",
                0,
                data.Title,
                "title"
            ),
            new DetectionScheme(
                @"(?<=℗ )[12]\d{3}(?=\s)",
                0,
                data.Description,
                "description's \"℗\" symbol"
            ),
            new DetectionScheme(
                @"(?<=[Rr]eleased [io]n: )[12]\d{3}",
                0,
                data.Description,
                "description 'released on' date"
            ),
            new DetectionScheme(
                @"[12]\d{3}(?=(?:[.\/年]\d{1,2}[.\/月]\d{1,2}日?\s?)?\s?(?:[Rr]elease|リリース|発売))",
                0,
                data.Description,
                "description's year-first–style date"
            ),
            new DetectionScheme(
                @"[12]\d{3}年(?=\d{1,2}月\d{1,2}日\s?[Rr]elease)",
                0,
                data.Description,
                "description's 年月日-style release date"
            ),
            new DetectionScheme(
                @"(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]",
                3,
                data.Title,
                "title"
            ),
            new DetectionScheme(
                @"(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]",
                3,
                data.Description,
                "description"
            ),
        };

        foreach (var pattern in parsePatterns)
        {
            var result = ParseYear(pattern.Regex, pattern.SourceText);
            if (result is null)
                continue;

            printer.Print($"• Writing year {result.Value} (matched via {pattern.Source})");
            return result.Value;
        }

        printer.Print($"• No year was parsed{(defaultYear is null ? "." : $", so defaulting to {defaultYear}.")}");
        return defaultYear;

        /// <summary>
        /// Applies a regex pattern against text, returning the matched value
        /// or else null if there was no successful match.
        /// </summary>
        /// <param name="regexPattern"></param>
        /// <param name="text">Text that might contain a year.</param>
        /// <returns>A number representing a year or null.</returns>
        static ushort? ParseYear(string regexPattern, string text)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(regexPattern);

            var regex = new Regex(regexPattern);
            var match = regex.Match(text);

            if (match is null)
                return null;
            return ushort.TryParse(match.Value, out var matchYear)
                ? matchYear
                : null;
        };
    }
}
