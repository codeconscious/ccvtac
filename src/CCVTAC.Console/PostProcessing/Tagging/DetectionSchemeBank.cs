namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Contains all detection schemes used to find tag data (artist, album, years, etc.)
/// within video metadata.
/// </summary>
/// <remarks>I might eventually move these to the user settings files instead.</remarks>
internal static class DetectionSchemeBank
{
    internal static IReadOnlyList<DetectionScheme> Title =
    [
        new(
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D""",
            MatchGroupId.First,
            SourceMetadataField.Description,
            "Topic style"
        ),
        new(
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroupId.First,
            SourceMetadataField.Description,
            "pseudo-Topic style"
        ),
        new(
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroupId.Second,
            SourceMetadataField.Title
        ),
        new(
            """(.+?) ['"](.+)['"]""",
            MatchGroupId.Second,
            SourceMetadataField.Title
        ),
        new(
            """^(.+?)「(.+)」""", // ARTIST「TITLE」 on one line
            MatchGroupId.Second,
            SourceMetadataField.Title
        ),
        new(
            """(.+) ?⧸ ?(.+)(?= ?：(?: \w+)?\.\w{3,4})""", // TITLE ⧸ ARTIST ：
            MatchGroupId.First,
            SourceMetadataField.Title
        ),
        new(
            """(.+) ⧸ (.+)(?=\.m4a)""", // ARTIST ⧸ TITLE
            MatchGroupId.Second,
            SourceMetadataField.Title
        ),
        new(
            """^【(.+)】(.+)$""", // 【ARTIST】TITLE
            MatchGroupId.Second,
            SourceMetadataField.Title
        ),
    ];

    internal static IReadOnlyList<DetectionScheme> Artist =
    [
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D""",
            MatchGroupId.Second,
            SourceMetadataField.Description,
            "Topic style"
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroupId.Second,
            SourceMetadataField.Description,
            "pseudo-Topic style"
        ),
        new (
            """(.+)(?: - )?[「『](.+)[」』]\[([12]\d{3})\]""",
            MatchGroupId.First,
            SourceMetadataField.Title
        ),
        new(
            """(.+?) ['"](.+)['"]""", // Artist 'TrackName'
            MatchGroupId.First,
            SourceMetadataField.Title
        ),
        new(
            """^(.+?)「(.+)」""", // ARTIST「TITLE」 on one line
            MatchGroupId.First,
            SourceMetadataField.Title
        ),
        new(
            """(.+) ?⧸ ?(.+)(?= ：(?: \w+)?\.\w{3,4})""", // TITLE ⧸ ARTIST ：
            MatchGroupId.Second,
            SourceMetadataField.Title
        ),
        new(
            """(.+) ⧸ (.+)(?=\.m4a)""", // ARTIST ⧸ TITLE
            MatchGroupId.First,
            SourceMetadataField.Title
        ),
        new(
            """^【(.+)】(.+)$""", // 【ARTIST】TITLE
            MatchGroupId.First,
            SourceMetadataField.Title
        ),
        new(
            """(?<=歌[:：]).+""", // 歌：○○○○ (Doesn't work with `$` appended.)
            MatchGroupId.Zero,
            SourceMetadataField.Description
        ),
    ];

    internal static IReadOnlyList<DetectionScheme> Album =
    [
        new (
            """(?<=[Aa]lbum: ).+""",
            MatchGroupId.Zero,
            SourceMetadataField.Description
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D""",
            MatchGroupId.Third,
            SourceMetadataField.Description,
            "Topic style"
        ),
        new (
            """(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗""",
            MatchGroupId.Third,
            SourceMetadataField.Description,
            "pseudo-Topic style"
        ),
        new (
            """(?<=(アルバム|シングル)[『「]).+?(?=[」』])""", // アルバム「NAME」
            MatchGroupId.Zero,
            SourceMetadataField.Title
        ),
        new (
            """(?<=(アルバム|シングル)[『「]).+?(?=[」』])""", // アルバム「NAME」
            MatchGroupId.Zero,
            SourceMetadataField.Description
        ),
        new (
            """(?<=EP[『「]).+?(?=[」』]収録)""", // EP「NAME」収録
            MatchGroupId.Zero,
            SourceMetadataField.Description
        ),
        new (
            """(?<='s ['"]).+(?=['"] album)""",
            MatchGroupId.Zero,
            SourceMetadataField.Description
        ),
        new (
            """(?<=Vol\.\d『).+(?=』)""",
            MatchGroupId.Zero,
            SourceMetadataField.Description
        ),
        new (
            """(?<=\w{3}アルバム『).+(?=』)""",
            MatchGroupId.Zero,
            SourceMetadataField.Description
        ),
    ];

    internal static IReadOnlyList<DetectionScheme> Composers =
    [
        new (
            """(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：・]).+""",
            MatchGroupId.Zero,
            SourceMetadataField.Description
        ),
        new (
            """(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：・]).+""",
            MatchGroupId.Zero,
            SourceMetadataField.Title
        )
    ];

    internal static IReadOnlyList<DetectionScheme> Year =
    [
        new (
            """(?<=[(（\[［【])[12]\d{3}(?=[)）\]］】])""",
            MatchGroupId.Zero,
            SourceMetadataField.Title
        ),
        new (
            """(?<=℗ )[12]\d{3}""",
            MatchGroupId.Zero,
            SourceMetadataField.Description,
            "℗\" symbol"
        ),
        new (
            """(?<=℗ )[12]\d{3}""",
            MatchGroupId.Zero,
            SourceMetadataField.Title,
            "℗\" symbol"
        ),
        new (
            """(?<=[Rr]eleased [io]n: )[12]\d{3}""",
            MatchGroupId.Zero,
            SourceMetadataField.Description,
            "'released on' date"
        ),
        new (
            """[12]\d{3}(?=(?:[.\/年]\d{1,2}[.\/月]\d{1,2}日?\s?)?\s?(?:[Rr]elease|リリース|発売))""",
            MatchGroupId.Zero,
            SourceMetadataField.Description,
            "'year first'-style year"
        ),
        new (
            """[12]\d{3}(?=年(?:\d{1,2}月\d{1,2}日)?(?:配信)?リリース)""",
            MatchGroupId.Zero,
            SourceMetadataField.Description,
            "YEAR年MONTH月DATE日配信リリース, YEAR年配信リリース, YEAR年リリース"
        ),
        new (
            """[12]\d{3}年(?=\d{1,2}月\d{1,2}日\s?[Rr]elease)""",
            MatchGroupId.Zero,
            SourceMetadataField.Description,
            "description's 年月日-style release date"
        ),
        new (
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroupId.Third,
            SourceMetadataField.Title
        ),
        new (
            """(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]""",
            MatchGroupId.Third,
            SourceMetadataField.Description
        ),
        new (
            """(?<=\(C\)\s|\(C\))[12]\d{3}""", // (C) 2000
            MatchGroupId.Zero,
            SourceMetadataField.Description
        ),
        new (
            """^(.+?)「(.+)」\s?([12]\d{3})\s?""", // ARTIST「TITLE」YEAR
            MatchGroupId.Third,
            SourceMetadataField.Title
        )
    ];
}
