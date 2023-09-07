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
    public string? DetectTitle(YouTubeJson.Root data, Printer printer, string? defaultName = null)
    {
        // TODO: Put this somewhere where it can be static.
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
                1,
                data.description,
                "description (Topic style)"
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

        printer.Print($"• Writing title \"{defaultName}\" (taken from video title)");
        return defaultName;
    }

    public string? DetectArtist(YouTubeJson.Root data, Printer printer, string? defaultName = null)
    {
        // TODO: Put this somewhere where it can be static.
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
                2,
                data.description,
                "description (Topic style)"
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

        return defaultName;
    }

    public string? DetectAlbum(YouTubeJson.Root data, Printer printer, string? defaultName = null)
    {
        // TODO: Put this somewhere where it can be static or else a setting.
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(?<=[Aa]lbum: ).+",
                0,
                data.description,
                "description"
            ),
            new DetectionScheme(
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
                3,
                data.description,
                "description (Topic style)"
            ),
            new DetectionScheme(
                """(?<='s ['"]).+(?=['"] album)""",
                0,
                data.description,
                "description"
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

        return defaultName;
    }

    public string? DetectComposer(YouTubeJson.Root data, Printer printer)
    {
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: ).+",
                0,
                data.description,
                "description"
            ),
            new DetectionScheme(
                @"(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: ).+",
                0,
                data.title,
                "title"
            )
        };

        foreach (var pattern in parsePatterns)
        {
            var regex = new Regex(pattern.Regex);
            var match = regex.Match(pattern.SourceText);

            if (!match.Success)
                continue;

            printer.Print($"• Writing composer \"{match.Groups[pattern.Group].Value}\" (matched via {pattern.Source})");
            return match.Groups[pattern.Group].Value.Trim();
        }

        return null;
    }

    /// <summary>
    /// Attempt to automatically detect a release year in the video metadata.
    /// If none is found, return a default value.
    /// </summary>
    public ushort? DetectReleaseYear(YouTubeJson.Root data, Printer printer, ushort? defaultYear = null)
    {
        // TODO: Feature: Use the video upload date for uploaders specified in user settings.
        // TODO: Put this somewhere where it can be static or made a setting.
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(?<=[(（\[［【])[12]\d{3}(?=[)）\]］】])",
                0,
                data.title,
                "title"
            ),
            new DetectionScheme(
                @"(?<=℗ )[12]\d{3}(?=\s)",
                0,
                data.description,
                "description's \"℗\" symbol"
            ),
            new DetectionScheme(
                @"(?<=[Rr]eleased [io]n: )[12]\d{3}",
                0,
                data.description,
                "description 'released on' date"
            ),
            new DetectionScheme(
                @"[12]\d{3}(?=年(?:\d{1,2}月\d{1,2}日)?リリース)",
                0,
                data.description,
                "description's リリース-style date"
            ),
            new DetectionScheme(
                @"[12]\d{3}年(?=\d{1,2}月\d{1,2}日\s?[Rr]elease)",
                0,
                data.description,
                "description's 年月日-style release date"
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

        printer.Print($"No year could be parsed{(defaultYear is null ? "." : $", so defaulting to {defaultYear}.")}");
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
