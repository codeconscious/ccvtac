namespace CCVTAC.Console.PostProcessing.Tagging;

internal class TagDetectionSchemes
{
    public string? DetectTitle(YouTubeVideoJson.Root data, Printer printer, string? defaultName = null)
    {
        // TODO: Put this, and similar blocks in this file, somewhere where it can be static.
        List<DetectionScheme> parsePatterns = new()
        {
            new DetectionScheme(
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
                1,
                data.Description,
                "description (Topic style)"
            ),
            new DetectionScheme(
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗",
                1,
                data.Description,
                "description (pseudo-Topic style)"
            ),
            new DetectionScheme(
                @"(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]",
                2,
                data.Title,
                "title"
            ),
        };

        string? output = Detectors.DetectSingle<string>(parsePatterns, null);

        if (output is null)
        {
            printer.Print($"• No title parsed.");
            return defaultName;
        }
        else
        {
            printer.Print($"• Found title \"{output}\"");
            return output;
        }
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
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗",
                2,
                data.Description,
                "description (pseudo-Topic style)"
            ),
            new DetectionScheme(
                @"(.+)(?: - )?[「『](.+)[」』]\[([12]\d{3})\]",
                1,
                data.Title,
                "title"
            ),
        };

        string? output = Detectors.DetectSingle<string>(parsePatterns, null);

        if (output is null)
        {
            printer.Print($"• No artist parsed.");
            return defaultName;
        }
        else
        {
            printer.Print($"• Found artist \"{output}\"");
            return output;
        }
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
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗",
                3,
                data.Description,
                "description (pseudo-Topic style)"
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
                "description"
            ),
        };

        var output = Detectors.DetectSingle<string>(parsePatterns, null);

        if (output is null)
        {
            printer.Print($"• No album parsed.");
            return defaultName;
        }
        else
        {
            printer.Print($"• Found album \"{output}\"");
            return output;
        }
    }

    /// <summary>
    /// Attempt to automatically detect a release year in the video metadata.
    /// If none is found, return a default value.
    /// </summary>
    public ushort? DetectReleaseYear(YouTubeVideoJson.Root data, Printer printer, ushort? defaultYear = null)
    {
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

        ushort output = Detectors.DetectSingle<ushort>(parsePatterns, default);

        if (output is default(ushort))
        {
            printer.Print($"• No year parsed.");
            return defaultYear;
        }
        else
        {
            printer.Print($"• Found year \"{output}\"");
            return output;
        }
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

        string? output = Detectors.DetectMultiple<string>(parsePatterns, null);

        if (output?.Any() == true)
        {
            var composerText = string.Join("; ", output);
            printer.Print($"• Found composer(s) \"{composerText}\"");
            return composerText;
        }
        else
        {
            printer.Print($"• No composers parsed.");
            return null;
        }
    }
}
