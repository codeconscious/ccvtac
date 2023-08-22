using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing;

internal static class Renamer
{
    private record struct RenamePattern(Regex Regex, string ReplacementText, string Description);

    private static readonly IReadOnlyList<RenamePattern> RenamePatterns = new List<RenamePattern>()
    {
        new RenamePattern(
            new Regex(@"\s\[[\w_-]{11}\](?=\.\w{3,5})"),
            string.Empty,
            "Remove trailing video IDs (recommend running this first)"),
        new RenamePattern(
            new Regex(@"\s{2,}"),
            " ",
            "Remove extra spaces"),
        new RenamePattern(
            new Regex(@"(?<= - )\d{3} (\d{1,3})\.?\s?"),
            "%<g1>s - ",
            "Remove and reformat duplicate track numbers"),
        new RenamePattern(
            new Regex(@"(.+?)(?: - )(.+?) \[[\w⧸]+\] .+ \(([\d\?？]{4})\)"),
            "%<g1>s - %<g2>s [%<g3>s]",
            "Custom reformat"),
        new RenamePattern(
            new Regex(@"\s*[(（【［\[\-]?(?:[Oo]fficial +|OFFICIAL +)?(?:[Mm]usic [Vv]ideo|MUSIC VIDEO|[Ll]yric [Vv]ideo|LYRIC VIDEO|[Vv]ideo|VIDEO|[Aa]udio|[Vv]isualizer|AUDIO|[Ff]ull (?:[Aa]lbum|LP|EP)|M(?:[_/])?V)[)】］）\]\-]?"),
            string.Empty,
            "Remove unneeded labels"),
        new RenamePattern(
            new Regex(@"^(.+?)(?: - )?\s?[｢「『【](.+)[」｣』】]\s?(\d{4})(?:\s?MV)?"),
            "%<g1>s - %<g2>s [%<g3>s]",
            "Reformat 'PERSON「TITLE」YEAR'"),
        new RenamePattern(
            new Regex(@"^(.+?)(?: - )?\s?[｢「『【](.+?)[」｣』】](?:\s?MV)?(?=\.\w{3,4})"),
            "%<g1>s - %<g2>s",
            "Reformat 'PERSON「TITLE」' (alone, not followed by anything)"),
        new RenamePattern(
            new Regex(@"^(.+?)(?: - )?\s?([｢「『【].+?[」｣』】](?:\s?MV)?.*)(?=\.\w{3,4})"),
            "%<g1>s - %<g2>s",
            "Reformat 'PERSON「TITLE」' followed by other info"),
        new RenamePattern(
            new Regex(@"(^.+) \[\s(.+)\s\]"),
            "%<g1>s - %<g2>s",
            "Reformat 'ARTIST [ TITLE ]'"),
        new RenamePattern(
            new Regex(@"^(.+)\s{1,}-\s{1,}'(.+)'"),
            "%<g1>s - %<g2>s",
            "Reformat 'ARTIST - \'TITLE\' ]'"),
        new RenamePattern(
            new Regex(@"^(.+?)(?: - [｢「『【])(.+)(?:[」｣』】]).*(?=（Full Ver.）)"),
            "%<g1>s - %<g2>s",
            "Reformat 'ARTIST - \'TITLE\' ]'"),
        new RenamePattern(
            new Regex(@" - - "),
            " - ",
            "Compress doubled hyphens"),
        new RenamePattern(
            new Regex(@" – "),
            " - ",
            "Replace en dashes with hyphens")
    };

    public static void Run(string workingDirectory, Printer printer)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var dir = new DirectoryInfo(workingDirectory);
        List<string> validExtensions = new() { ".m4a" };

        var files = dir.EnumerateFiles("*") // Needed?
                       .Where(f => validExtensions.Contains(f.Extension));
        printer.Print($"Renaming {files.Count()} {string.Join(" and ", validExtensions)} file(s)...");

        foreach (var file in files)
        {
            var workingFileName = new StringBuilder(file.Name);
            foreach(var pattern in RenamePatterns)
            {
                var match = pattern.Regex.Match(workingFileName.ToString());
                if (!match.Success)
                    continue;

                workingFileName.Remove(match.Index, match.Length);

                var replacementText = new StringBuilder(pattern.ReplacementText);
                for (int i = 0; i < match.Groups.Count - 1; i++)
                {
                    replacementText.Replace($"%<g{i + 1}>s", match.Groups[i + 1].Value);
                }

                workingFileName.Insert(match.Index, replacementText.ToString());
            }

            try
            {
                File.Move(
                    file.FullName,
                    Path.Combine(workingDirectory, workingFileName.ToString()));
                printer.Print($"- Renamed \"{file.Name}\" to \"{workingFileName}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"Could not rename \"{file.Name}\": {ex.Message}");
            }
        }

        printer.Print($"Renaming done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }
}
