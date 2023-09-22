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
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D""",
            MatchGroup.First,
            SourceField.Description,
            "Topic style"
        ),
        new(
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroup.First,
            SourceField.Description,
            "pseudo-Topic style"
        ),
        new(
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroup.Second,
            SourceField.Title
        ),
        new(
            """(.+?) ['"](.+)['"]""",
            MatchGroup.Second,
            SourceField.Title
        ),
        new(
            """^(.+?)「(.+)」""", // ARTIST「TITLE」 on one line
            MatchGroup.Second,
            SourceField.Title
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Artist = new List<DetectionScheme>()
    {
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D""",
            MatchGroup.Second,
            SourceField.Description,
            "Topic style"
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroup.Second,
            SourceField.Description,
            "pseudo-Topic style"
        ),
        new (
            """(.+)(?: - )?[「『](.+)[」』]\[([12]\d{3})\]""",
            MatchGroup.First,
            SourceField.Title
        ),
        new(
            """(.+?) ['"](.+)['"]""", // Artist 'TrackName'
            MatchGroup.First,
            SourceField.Title
        ),
        new(
            """^(.+?)「(.+)」""", // ARTIST「TITLE」 on one line
            MatchGroup.First,
            SourceField.Title
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Album = new List<DetectionScheme>()
    {
        new (
            """(?<=[Aa]lbum: ).+""",
            MatchGroup.Zero,
            SourceField.Description
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D""",
            MatchGroup.Third,
            SourceField.Description,
            "Topic style"
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroup.Third,
            SourceField.Description,
            "pseudo-Topic style"
        ),
        new (
            """(?<='s ['"]).+(?=['"] album)""",
            MatchGroup.Zero,
            SourceField.Description
        ),
        new (
            """(?<=Vol\.\d『).+(?=』)""",
            MatchGroup.Zero,
            SourceField.Description
        ),
        new (
            """(?<=^\w{3}アルバム『).+(?=』)""",
            MatchGroup.Zero,
            SourceField.Description
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Composers = new List<DetectionScheme>()
    {
        new (
            """(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+""",
            MatchGroup.Zero,
            SourceField.Description
        ),
        new (
            """(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+""",
            MatchGroup.Zero,
            SourceField.Title
        )
    };

    internal static IReadOnlyList<DetectionScheme> Year = new List<DetectionScheme>()
    {
        new (
            """(?<=[(（\[［【])[12]\d{3}(?=[)）\]］】])""",
            MatchGroup.Zero,
            SourceField.Title
        ),
        new (
            """(?<=℗ )[12]\d{3}(?=\s)""",
            MatchGroup.Zero,
            SourceField.Description,
            "℗\" symbol"
        ),
        new (
            """(?<=[Rr]eleased [io]n: )[12]\d{3}""",
            MatchGroup.Zero,
            SourceField.Description,
            "'released on' date"
        ),
        new (
            """[12]\d{3}(?=(?:[.\/年]\d{1,2}[.\/月]\d{1,2}日?\s?)?\s?(?:[Rr]elease|リリース|発売))""",
            MatchGroup.Zero,
            SourceField.Description,
            "'year first'-style year"
        ),
        new (
            """[12]\d{3}年(?=\d{1,2}月\d{1,2}日\s?[Rr]elease)""",
            MatchGroup.Zero,
            SourceField.Description,
            "description's 年月日-style release date"
        ),
        new (
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroup.Third,
            SourceField.Title
        ),
        new (
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroup.Third,
            SourceField.Description
        )
    };
}
