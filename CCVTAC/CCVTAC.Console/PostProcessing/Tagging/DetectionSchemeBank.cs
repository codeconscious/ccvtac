namespace CCVTAC.Console.PostProcessing.Tagging;

public static class DetectionSchemeBank
{
    public static IReadOnlyList<DetectionScheme> Title = new List<DetectionScheme>()
    {
        new(
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
            1,
            DetectionTarget.Description,
            "Topic style"
        ),
        new(
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗",
            1,
            DetectionTarget.Description,
            "pseudo-Topic style"
        ),
        new(
            @"(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]",
            2,
            DetectionTarget.Title,
            "title"
        ),
    };

    public static IReadOnlyList<DetectionScheme> Artist = new List<DetectionScheme>()
    {
        new (
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
            2,
            DetectionTarget.Description,
            "Topic style"
        ),
        new (
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗",
            2,
            DetectionTarget.Description,
            "pseudo-Topic style"
        ),
        new (
            @"(.+)(?: - )?[「『](.+)[」』]\[([12]\d{3})\]",
            1,
            DetectionTarget.Title
        ),
    };

    public static IReadOnlyList<DetectionScheme> Album = new List<DetectionScheme>()
    {
        new (
            @"(?<=[Aa]lbum: ).+",
            0,
            DetectionTarget.Description
        ),
        new (
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
            3,
            DetectionTarget.Description,
            "Topic style"
        ),
        new (
            @"(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗",
            3,
            DetectionTarget.Description,
            "pseudo-Topic style"
        ),
        new (
            """(?<='s ['"]).+(?=['"] album)""",
            0,
            DetectionTarget.Description
        ),
        new (
            """(?<=Vol\.\d『).+(?=』\s?#\d)""",
            0,
            DetectionTarget.Description
        ),
        new (
            """(?<=^\w{3}アルバム『).+(?=』)""",
            0,
            DetectionTarget.Description
        ),
    };

    public static IReadOnlyList<DetectionScheme> Composers = new List<DetectionScheme>()
    {
        new (
            @"(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+",
            0,
            DetectionTarget.Description
        ),
        new (
            @"(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：]).+",
            0,
            DetectionTarget.Title
        )
    };

    public static IReadOnlyList<DetectionScheme> Year = new List<DetectionScheme>()
    {
        new (
            @"(?<=[(（\[［【])[12]\d{3}(?=[)）\]］】])",
            0,
            DetectionTarget.Title
        ),
        new (
            @"(?<=℗ )[12]\d{3}(?=\s)",
            0,
            DetectionTarget.Description,
            "℗\" symbol"
        ),
        new (
            @"(?<=[Rr]eleased [io]n: )[12]\d{3}",
            0,
            DetectionTarget.Description,
            "'released on' date"
        ),
        new (
            @"[12]\d{3}(?=(?:[.\/年]\d{1,2}[.\/月]\d{1,2}日?\s?)?\s?(?:[Rr]elease|リリース|発売))",
            0,
            DetectionTarget.Description,
            "'year first'-style year"
        ),
        new (
            @"[12]\d{3}年(?=\d{1,2}月\d{1,2}日\s?[Rr]elease)",
            0,
            DetectionTarget.Description,
            "description's 年月日-style release date"
        ),
        new (
            @"(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]",
            3,
            DetectionTarget.Title
        ),
        new (
            @"(.+) (?:\d\w{2}|Vol\.\d)?『(.+)』\[([12]\d{3})\]",
            3,
            DetectionTarget.Description
        )
    };
}
