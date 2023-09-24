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
            SourceMetadataField.Description,
            "Topic style"
        ),
        new(
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroup.First,
            SourceMetadataField.Description,
            "pseudo-Topic style"
        ),
        new(
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroup.Second,
            SourceMetadataField.Title
        ),
        new(
            """(.+?) ['"](.+)['"]""",
            MatchGroup.Second,
            SourceMetadataField.Title
        ),
        new(
            """^(.+?)「(.+)」""", // ARTIST「TITLE」 on one line
            MatchGroup.Second,
            SourceMetadataField.Title
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Artist = new List<DetectionScheme>()
    {
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D""",
            MatchGroup.Second,
            SourceMetadataField.Description,
            "Topic style"
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroup.Second,
            SourceMetadataField.Description,
            "pseudo-Topic style"
        ),
        new (
            """(.+)(?: - )?[「『](.+)[」』]\[([12]\d{3})\]""",
            MatchGroup.First,
            SourceMetadataField.Title
        ),
        new(
            """(.+?) ['"](.+)['"]""", // Artist 'TrackName'
            MatchGroup.First,
            SourceMetadataField.Title
        ),
        new(
            """^(.+?)「(.+)」""", // ARTIST「TITLE」 on one line
            MatchGroup.First,
            SourceMetadataField.Title
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Album = new List<DetectionScheme>()
    {
        new (
            """(?<=[Aa]lbum: ).+""",
            MatchGroup.Zero,
            SourceMetadataField.Description
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D""",
            MatchGroup.Third,
            SourceMetadataField.Description,
            "Topic style"
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroup.Third,
            SourceMetadataField.Description,
            "pseudo-Topic style"
        ),
        new (
            """(?<='s ['"]).+(?=['"] album)""",
            MatchGroup.Zero,
            SourceMetadataField.Description
        ),
        new (
            """(?<=Vol\.\d『).+(?=』)""",
            MatchGroup.Zero,
            SourceMetadataField.Description
        ),
        new (
            """(?<=^\w{3}アルバム『).+(?=』)""",
            MatchGroup.Zero,
            SourceMetadataField.Description
        ),
    };

    internal static IReadOnlyList<DetectionScheme> Composers = new List<DetectionScheme>()
    {
        new (
            """(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+""",
            MatchGroup.Zero,
            SourceMetadataField.Description
        ),
        new (
            """(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+""",
            MatchGroup.Zero,
            SourceMetadataField.Title
        )
    };

    internal static IReadOnlyList<DetectionScheme> Year = new List<DetectionScheme>()
    {
        new (
            """(?<=[(（\[［【])[12]\d{3}(?=[)）\]］】])""",
            MatchGroup.Zero,
            SourceMetadataField.Title
        ),
        new (
            """(?<=℗ )[12]\d{3}(?=\s)""",
            MatchGroup.Zero,
            SourceMetadataField.Description,
            "℗\" symbol"
        ),
        new (
            """(?<=[Rr]eleased [io]n: )[12]\d{3}""",
            MatchGroup.Zero,
            SourceMetadataField.Description,
            "'released on' date"
        ),
        new (
            """[12]\d{3}(?=(?:[.\/年]\d{1,2}[.\/月]\d{1,2}日?\s?)?\s?(?:[Rr]elease|リリース|発売))""",
            MatchGroup.Zero,
            SourceMetadataField.Description,
            "'year first'-style year"
        ),
        new (
            """[12]\d{3}年(?=\d{1,2}月\d{1,2}日\s?[Rr]elease)""",
            MatchGroup.Zero,
            SourceMetadataField.Description,
            "description's 年月日-style release date"
        ),
        new (
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroup.Third,
            SourceMetadataField.Title
        ),
        new (
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroup.Third,
            SourceMetadataField.Description
        )
    };
}
