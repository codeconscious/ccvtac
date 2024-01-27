using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing;

internal static class Renamer
{
    private readonly record struct RenamePattern(Regex Regex, string ReplaceWithPattern, string? Description = null);

    // TODO: Convert this into a setting.
    private static readonly IReadOnlyList<RenamePattern> RenamePatterns = new List<RenamePattern>()
    {
        // Universal, to always be run first.
        new(
            new Regex(@"\s\[[\w_-]{11}\](?=\.\w{3,5})"),
            string.Empty,
            "Remove trailing video IDs (recommend running this first)"),
        new(
            new Regex(@"\s{2,}"),
            " ",
            "Remove extra spaces"),
        // Various patterns
        new(
            new Regex(@"(?<= - )\d{3} (\d{1,3})\.?\s?"),
            "%<1>s - ",
            "Remove and reformat duplicate track numbers"),
        new(
            new Regex(@"\s*[(（【［\[\-]?(?:[Oo]fficial +|OFFICIAL +)?(?:HD )?(?:[Mm]usic [Vv]ideo|MUSIC VIDEO|[Ll]yric [Vv]ideo|LYRIC VIDEO|[Vv]ideo|VIDEO|[Aa]udio|[Vv]isualizer|AUDIO|[Ff]ull (?:[Aa]lbum|LP|EP)|M(?:[_/])?V)[)】］）\]\-]?"),
            string.Empty,
            "Remove unneeded labels"),
        new(
            new Regex("""\s?[\(（]歌詞入り[\)）]"""),
            string.Empty,
            "Remove \" (歌詞入り)\""),
        new(
            new Regex("""【(.+)】(.+)"""), // 【person】title
            "%<1>s - %<2>s",
            "PERSON - TRACK"),
        new(
            new Regex(@"(.+?)(?: - )(.+?) \[[\w⧸]+\] .+ \(([\d\?？]{4})\)"),
            "%<1>s - %<2>s [%<3>s]",
            "PERSON - TRACK [YEAR]"),
        new(
            new Regex(@"^(.+?)(?: - )?\s?[｢「『【](.+)[」｣』】]\s?\[?([12]\d{3})\]?(?:\s?MV)?"),
            "%<1>s - %<2>s [%<3>s]",
            "Reformat 'PERSON「TITLE」YEAR' and 'PERSON「TITLE」[YEAR]'"),
        new(
            new Regex(@"^(.+?)(?: - )?\s?[｢「『【](.+?)[」｣』】](?:\s?MV)?(?=\.\w{3,4})"),
            "%<1>s - %<2>s",
            "Reformat 'PERSON「TITLE」' (alone, not followed by anything)"),
        new(
            new Regex(@"^(.+?)(?: - )?\s?([｢「『【].+?[」｣』】](?:\s?MV)?.*)(?=\.\w{3,4})"),
            "%<1>s - %<2>s",
            "Reformat 'PERSON「TITLE」' followed by other info"),
        new(
            new Regex(@"(^.+) \[\s(.+)\s\]"),
            "%<1>s - %<2>s",
            "Reformat 'ARTIST [ TITLE ]'"),
        new(
            new Regex(@"^(.+)\s{1,}-\s{1,}['＂](.+)['＂]"),
            "%<1>s - %<2>s",
            """Reformat 'ARTIST - 'TITLE' ]', etc."""),
        new(
            new Regex(@"^(.+?)(?: - [｢「『【])(.+)(?:[」｣』】]).*(?=（Full Ver.）)"),
            "%<1>s - %<2>s",
            "Reformat 'ARTIST - \'TITLE\' ]'"),
        new(
            new Regex(@"(\d+) - \[(feat.+)\] (.+) ⧸ (.+)(?=\.\w{3,4})"),
            "%<4>s - %<4>s - %<1>s - %<3>s (%<2>s)"
        ),
        new(
            new Regex(@"(.+) ?⧸ ?(.+)(?= ：(?: \w+)?\.\w{3,4})"),
            "%<2>s - %<1>s"
        ),
        // Cleanup:
        new(
            new Regex(@" - - "),
            " - ",
            "Compress doubled hyphens"),
        new(
            new Regex(@" – "),
            " - ",
            "Replace en dashes with hyphens")
    };

    public static void Run(string workingDirectory, Printer printer)
    {
        Watch watch = new();

        DirectoryInfo dir = new(workingDirectory);

        var audioFilePaths = dir.EnumerateFiles("*.m4a");
        if (!audioFilePaths.Any())
        {
            printer.Warning("No audio files to rename were found.");
            return;
        }

        printer.Print($"Renaming {audioFilePaths.Count()} audio file(s)...");

        foreach (FileInfo filePath in audioFilePaths)
        {
            string newFileName = RenamePatterns.Aggregate(
                new StringBuilder(filePath.Name),
                (newFileNameSb, renamePattern) =>
                {
                    // Only continue if the current regex is a match.
                    var match = renamePattern.Regex.Match(newFileNameSb.ToString());
                    if (!match.Success)
                        return newFileNameSb;

                    // Delete the matched substring by index.
                    newFileNameSb.Remove(match.Index, match.Length);

                    // Work out the replacement text that should be inserted.
                    string insertText =
                        match.Groups.OfType<Group>()
                             .Select((gr, i) =>
                             (
                                SearchFor:   $"%<{i + 1}>s",
                                ReplaceWith: match.Groups[i + 1].Value
                             ))
                             .Aggregate(
                                 new StringBuilder(renamePattern.ReplaceWithPattern),
                                 (workingText, replacementParts) =>
                                     workingText.Replace(
                                        replacementParts.SearchFor,
                                        replacementParts.ReplaceWith),
                                 workingText => workingText.ToString()
                             );

                    // Insert the replacement text at the same starting position.
                    newFileNameSb.Insert(match.Index, insertText);

                    return newFileNameSb;
                },
                newFileNameSb => newFileNameSb.ToString());

            try
            {
                File.Move(
                    filePath.FullName,
                    Path.Combine(workingDirectory, newFileName));
                printer.Print($"• From: \"{filePath.Name}\"");
                printer.Print($"    To: \"{newFileName}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"• Could not rename \"{filePath.Name}\": {ex.Message}");
            }
        }

        printer.Print($"Renaming done in {watch.ElapsedFriendly}.");
    }
}
