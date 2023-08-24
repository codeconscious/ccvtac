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
        printer.Print("- Easily convert YouTube videos to local M4A audio files!");
        printer.Print("- Supports video and playlist URLs");
        printer.Print("- Video metadata (uploader name and URL, source URL, etc.) saved to Comment tags");
        printer.Print("- Renames files via specific regex patterns (to remove resource IDs, etc.)");
        printer.Print("- Video thumbnails are auto-trimmed and written to files as album art (Optional)");
        printer.Print("- Post-processed files are automatically moved to a specified directory");
        printer.Print("- All URLs entered are saved locally to a file named `history.log`", appendLines: 1);

        printer.Print("Instructions:");
        printer.Print("• Run the program once to generate a blank settings.json file, then populate it with directory paths.");
        printer.Print("• After the application starts, enter single URLs to start the download process.");
        printer.Print("  URLs may be for single videos or playlists.");
        printer.Print("• Enter `q` or `quit` to quit.");
    }
}
