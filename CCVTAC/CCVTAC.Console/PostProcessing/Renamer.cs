using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing;

internal static class Renamer
{
    private record struct RenamePattern(Regex Regex, string ReplacementText, string Description);

    // TODO: Convert this into a setting.
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
            "%<1>s - ",
            "Remove and reformat duplicate track numbers"),
        new RenamePattern(
            new Regex(@"(.+?)(?: - )(.+?) \[[\w⧸]+\] .+ \(([\d\?？]{4})\)"),
            "%<1>s - %<2>s [%<3>s]",
            "Custom reformat"),
        new RenamePattern(
            new Regex(@"\s*[(（【［\[\-]?(?:[Oo]fficial +|OFFICIAL +)?(?:[Mm]usic [Vv]ideo|MUSIC VIDEO|[Ll]yric [Vv]ideo|LYRIC VIDEO|[Vv]ideo|VIDEO|[Aa]udio|[Vv]isualizer|AUDIO|[Ff]ull (?:[Aa]lbum|LP|EP)|M(?:[_/])?V)[)】］）\]\-]?"),
            string.Empty,
            "Remove unneeded labels"),
        new RenamePattern(
            new Regex(@"^(.+?)(?: - )?\s?[｢「『【](.+)[」｣』】]\s?(\d{4})(?:\s?MV)?"),
            "%<1>s - %<2>s [%<3>s]",
            "Reformat 'PERSON「TITLE」YEAR'"),
        new RenamePattern(
            new Regex(@"^(.+?)(?: - )?\s?[｢「『【](.+?)[」｣』】](?:\s?MV)?(?=\.\w{3,4})"),
            "%<1>s - %<2>s",
            "Reformat 'PERSON「TITLE」' (alone, not followed by anything)"),
        new RenamePattern(
            new Regex(@"^(.+?)(?: - )?\s?([｢「『【].+?[」｣』】](?:\s?MV)?.*)(?=\.\w{3,4})"),
            "%<1>s - %<2>s",
            "Reformat 'PERSON「TITLE」' followed by other info"),
        new RenamePattern(
            new Regex(@"(^.+) \[\s(.+)\s\]"),
            "%<1>s - %<2>s",
            "Reformat 'ARTIST [ TITLE ]'"),
        new RenamePattern(
            new Regex(@"^(.+)\s{1,}-\s{1,}'(.+)'"),
            "%<1>s - %<2>s",
            "Reformat 'ARTIST - \'TITLE\' ]'"),
        new RenamePattern(
            new Regex(@"^(.+?)(?: - [｢「『【])(.+)(?:[」｣』】]).*(?=（Full Ver.）)"),
            "%<1>s - %<2>s",
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

        var files = dir.EnumerateFiles("*") // Needed?
                       .Where(f => Settings.SettingsService.ValidAudioFormats.Any(f.Extension.EndsWith));
        printer.Print($"Renaming {files.Count()} audio file(s)...");

        foreach (var file in files)
        {
            var workingNewFileName = new StringBuilder(file.Name);
            foreach(var pattern in RenamePatterns)
            {
                var match = pattern.Regex.Match(workingNewFileName.ToString());
                if (!match.Success)
                    continue;

                workingNewFileName.Remove(match.Index, match.Length);

                var replacementText = new StringBuilder(pattern.ReplacementText);
                for (int i = 0; i < match.Groups.Count - 1; i++)
                {
                    replacementText.Replace($"%<{i + 1}>s", match.Groups[i + 1].Value);
                }

                workingNewFileName.Insert(match.Index, replacementText.ToString());
            }

            try
            {
                File.Move(
                    file.FullName,
                    Path.Combine(workingDirectory, workingNewFileName.ToString()));
                printer.Print($"- From: \"{file.Name}\"");
                printer.Print($"    To: \"{workingNewFileName}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"Could not rename \"{file.Name}\": {ex.Message}");
            }
        }

        printer.Print($"Renaming done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }
}
