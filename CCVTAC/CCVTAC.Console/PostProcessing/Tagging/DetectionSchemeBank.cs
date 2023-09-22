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
            MatchGroup.Group1,
            SourceField.Description,
            "Topic style"
        ),
        new(
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroup.Group1,
            SourceField.Description,
            "pseudo-Topic style"
        ),
        new(
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroup.Group2,
            SourceField.Title
        ),
        new(
            """(.+?) ['"](.+)['"]""",
            MatchGroup.Group2,
            SourceField.Title
        ),
        new(
            """^(.+?)「(.+)」""", // ARTIST「TITLE」 on one line
            MatchGroup.Group2,
            SourceField.Title
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Artist = new List<DetectionScheme>()
    {
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D""",
            MatchGroup.Group2,
            SourceField.Description,
            "Topic style"
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroup.Group2,
            SourceField.Description,
            "pseudo-Topic style"
        ),
        new (
            """(.+)(?: - )?[「『](.+)[」』]\[([12]\d{3})\]""",
            MatchGroup.Group1,
            SourceField.Title
        ),
        new(
            """(.+?) ['"](.+)['"]""", // Artist 'TrackName'
            MatchGroup.Group1,
            SourceField.Title
        ),
        new(
            """^(.+?)「(.+)」""", // ARTIST「TITLE」 on one line
            MatchGroup.Group1,
            SourceField.Title
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Album = new List<DetectionScheme>()
    {
        new (
            """(?<=[Aa]lbum: ).+""",
            MatchGroup.Group0,
            SourceField.Description
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D""",
            MatchGroup.Group3,
            SourceField.Description,
            "Topic style"
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroup.Group3,
            SourceField.Description,
            "pseudo-Topic style"
        ),
        new (
            """(?<='s ['"]).+(?=['"] album)""",
            MatchGroup.Group0,
            SourceField.Description
        ),
        new (
            """(?<=Vol\.\d『).+(?=』)""",
            MatchGroup.Group0,
            SourceField.Description
        ),
        new (
            """(?<=^\w{3}アルバム『).+(?=』)""",
            MatchGroup.Group0,
            SourceField.Description
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Composers = new List<DetectionScheme>()
    {
        new (
            """(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+""",
            MatchGroup.Group0,
            SourceField.Description
        ),
        new (
            """(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+""",
            MatchGroup.Group0,
            SourceField.Title
        )
    };

    internal static IReadOnlyList<DetectionScheme> Year = new List<DetectionScheme>()
    {
        new (
            """(?<=[(（\[［【])[12]\d{3}(?=[)）\]］】])""",
            MatchGroup.Group0,
            SourceField.Title
        ),
        new (
            """(?<=℗ )[12]\d{3}(?=\s)""",
            MatchGroup.Group0,
            SourceField.Description,
            "℗\" symbol"
        ),
        new (
            """(?<=[Rr]eleased [io]n: )[12]\d{3}""",
            MatchGroup.Group0,
            SourceField.Description,
            "'released on' date"
        ),
        new (
            """[12]\d{3}(?=(?:[.\/年]\d{1,2}[.\/月]\d{1,2}日?\s?)?\s?(?:[Rr]elease|リリース|発売))""",
            MatchGroup.Group0,
            SourceField.Description,
            "'year first'-style year"
        ),
        new (
            """[12]\d{3}年(?=\d{1,2}月\d{1,2}日\s?[Rr]elease)""",
            MatchGroup.Group0,
            SourceField.Description,
            "description's 年月日-style release date"
        ),
        new (
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroup.Group3,
            SourceField.Title
        ),
        new (
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroup.Group3,
            SourceField.Description
        )
    };
}
