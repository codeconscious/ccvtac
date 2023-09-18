namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Contains all detection schemes used to find tag data (artist, album, years, etc.)
/// within video metadata.
/// </summary>
/// <remarks>I might eventually move these to the user settings files instead.</remarks>
internal static class DetectionSchemeBank
{
    internal static IReadOnlyList<DetectionScheme> Title = new List<DetectionScheme>()
    {
        new(
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
            1,
            SourceField.Description,
            "Topic style"
        ),
        new(
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗",
            1,
            SourceField.Description,
            "pseudo-Topic style"
        ),
        new(
            @"(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]",
            2,
            SourceField.Title,
            "title"
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Artist = new List<DetectionScheme>()
    {
        new (
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
            2,
            SourceField.Description,
            "Topic style"
        ),
        new (
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗",
            2,
            SourceField.Description,
            "pseudo-Topic style"
        ),
        new (
            @"(.+)(?: - )?[「『](.+)[」』]\[([12]\d{3})\]",
            1,
            SourceField.Title
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Album = new List<DetectionScheme>()
    {
        new (
            @"(?<=[Aa]lbum: ).+",
            0,
            SourceField.Description
        ),
        new (
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
            3,
            SourceField.Description,
            "Topic style"
        ),
        new (
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗",
            3,
            SourceField.Description,
            "pseudo-Topic style"
        ),
        new (
            """(?<='s ['"]).+(?=['"] album)""",
            0,
            SourceField.Description
        ),
        new (
            """(?<=Vol\.\d『).+(?=』\s?#\d)""",
            0,
            SourceField.Description
        ),
        new (
            """(?<=^\w{3}アルバム『).+(?=』)""",
            0,
            SourceField.Description
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Composers = new List<DetectionScheme>()
    {
        new (
            @"(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+",
            0,
            SourceField.Description
        ),
        new (
            @"(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+",
            0,
            SourceField.Title
        )
    };

    internal static IReadOnlyList<DetectionScheme> Year = new List<DetectionScheme>()
    {
        new (
            @"(?<=[(（\[［【])[12]\d{3}(?=[)）\]］】])",
            0,
            SourceField.Title
        ),
        new (
            @"(?<=℗ )[12]\d{3}(?=\s)",
            0,
            SourceField.Description,
            "℗\" symbol"
        ),
        new (
            @"(?<=[Rr]eleased [io]n: )[12]\d{3}",
            0,
            SourceField.Description,
            "'released on' date"
        ),
        new (
            @"[12]\d{3}(?=(?:[.\/年]\d{1,2}[.\/月]\d{1,2}日?\s?)?\s?(?:[Rr]elease|リリース|発売))",
            0,
            SourceField.Description,
            "'year first'-style year"
        ),
        new (
            @"[12]\d{3}年(?=\d{1,2}月\d{1,2}日\s?[Rr]elease)",
            0,
            SourceField.Description,
            "description's 年月日-style release date"
        ),
        new (
            @"(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]",
            3,
            SourceField.Title
        ),
        new (
            @"(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]",
            3,
            SourceField.Description
        )
    };
}
