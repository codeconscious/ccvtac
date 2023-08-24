using System.IO;
using CCVTAC.Console.DownloadEntities;
using CCVTAC.Console.Settings;

namespace CCVTAC.Console;

class Program
{
    private static readonly string[] HelpCommands = new[] { "-h", "--help" };
    private static readonly string[] QuitCommands = new[] { "q", "quit", "exit" };

    static void Main(string[] args)
    {
        var printer = new Printer();

        if (args.Length > 0 && HelpCommands.Contains(args[0].ToLowerInvariant()))
        {
            PrintHelp(printer);
            return;
        }

        var readSettingsResult = SettingsService.Read(printer, createFileIfMissing: true);
        if (readSettingsResult.IsFailed)
        {
            printer.Errors(
                readSettingsResult.Errors.Select(e => e.Message),
                "Error(s) reading the settings file:");
            return;
        }
        Settings.Settings settings = readSettingsResult.Value;

        var ensureValidSettingsResult = SettingsService.EnsureValidSettings(settings);
        if (ensureValidSettingsResult.IsFailed)
        {
            printer.Errors(
                ensureValidSettingsResult.Errors.Select(e => e.Message),
                "Error(s) found in settings file:");
            return;
        }
        printer.Print("Settings file loaded OK.");

        SettingsService.SetId3v2Version(
            version: SettingsService.Id3v2Version.TwoPoint3,
            forceAsDefault: true);

        const string prompt = "Enter a YouTube URL (or 'q' to quit): ";
        ushort successCount = 0;
        ushort failureCount = 0;
        while (true)
        {
            string input = printer.GetInput(prompt);

            if (QuitCommands.Contains(input.ToLowerInvariant()))
            {
                var successLabel = successCount == 1 ? "success" : "successes";
                var failureLabel = failureCount == 1 ? "failure" : "failures";
                printer.Print($"Quitting with {successCount} {successLabel} and {failureCount} {failureLabel}.");
                return;
            }

            var result = Run(input, settings, printer);
            if (result.IsSuccess)
            {
                successCount++;
                printer.Print(result.Value, appendLines: 1);
            }
            else
            {
                failureCount++;
                printer.Error(result.Value, appendLines: 1);
            }
        }
    }

    static Result<string> Run(string url, Settings.Settings settings, Printer printer)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var downloadEntityResult = DownloadEntityFactory.Create(url);
        if (downloadEntityResult.IsFailed)
        {
            return Result.Fail(downloadEntityResult.Errors?.First().Message
                               ?? "An unknown error occurred parsing the resource type.");
        }
        var downloadEntity = downloadEntityResult.Value;
        printer.Print($"Processing {downloadEntity.GetType()} URL...");

        IReadOnlyList<string> args = new List<string>() {
            "--extract-audio -f m4a",
            "--write-thumbnail",
            "--convert-thumbnails jpg",
            "--write-info-json",
            "--split-chapters" // 設定ファイルの項目にするかもしれない。
        };

        var downloadResult = ExternalTools.Downloader(
            string.Join(" ", args),
            downloadEntity,
            settings.WorkingDirectory!,
            printer);
        if (downloadResult.IsFailed)
        {
            return Result.Fail(downloadResult.Errors.First().Message);
        }

        try
        {
            File.AppendAllText("history.log", url + Environment.NewLine);
            printer.Print("Added URL to the history log.");
        }
        catch (Exception ex)
        {
            printer.Error($"Could not append URL {url} to history log: " + ex.Message);
        }

        var postProcessor = new PostProcessing.Setup(settings, printer);
        postProcessor.Run();

        return Result.Ok($"Done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }

    static void PrintHelp(Printer printer)
    {
        printer.Print("CodeConscious Video-to-Audio Converter (ccvtac)");
        printer.Print("• Converts YouTube videos to M4A audio files saved locally on your device");
        printer.Print("• Is a wrapper around yt-dlp (https://github.com/yt-dlp/yt-dlp/), which must already be installed");
        printer.Print("• Adds ID3v2 tags, including video thumbnails as album art and a comment summarizing video metadata");
        printer.Print("• Keeps a local-only history of entered URLs");
        printer.Print("Instructions:");
        // printer.Print("• When starting the program, optionally supply one or more of the following argument(s):");
        // printer.Print("    -s   Splits video chapters into separate files (Continues until you quit)");
        //                     -D   Debug mode, in which no downloads occur
        printer.Print("• After the application starts, enter single URLs to start the download and conversion process.");
        printer.Print("  URLs may be for single videos, playlists, or channels.");
        printer.Print("• Enter `q` or `quit` to quit.");
    }
}
