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

        var settingsResult = GetSettings();
        if (settingsResult.IsFailed)
        {
            printer.Errors(settingsResult.Errors.Select(e => e.Message), "Settings file errors:");
            return;
        }
        var settings = settingsResult.Value;
        SettingsService.PrintSummary(settings, printer, "Settings loaded OK:");

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
                printer.Print(result.Value);
            }
            else
            {
                failureCount++;
                printer.Error(result.Value);
            }
        }
    }

    static Result<UserSettings> GetSettings()
    {
        var readSettingsResult = SettingsService.Read(createFileIfMissing: true);
        if (readSettingsResult.IsFailed)
            return Result.Fail(readSettingsResult.Errors.Select(e => e.Message));

        UserSettings settings = readSettingsResult.Value;

        var ensureValidSettingsResult = SettingsService.EnsureValidSettings(settings);
        if (ensureValidSettingsResult.IsFailed)
        {
            return ensureValidSettingsResult;
        }

        return readSettingsResult.Value;
    }

    static Result<string> Run(string url, UserSettings settings, Printer printer)
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

        var downloadToolSettings = new ExternalTools.ExternalToolSettings(
            "video download and audio extraction",
            "yt-dlp",
            GenerateDownloadArgs(settings, url),
            settings.WorkingDirectory!,
            printer
        );
        var downloadResult = ExternalTools.Run(downloadToolSettings);
        if (downloadResult.IsFailed)
        {
            downloadResult.Errors.ForEach(e => printer.Error(e.Message));
            printer.Warning("However, post-processing will still be attempted.");
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

    private static string GenerateDownloadArgs(UserSettings settings, params string[]? additionalArgs)
    {
        var args = new List<string>() {
            $"--extract-audio -f {settings.AudioFormat}",
            "--write-thumbnail --convert-thumbnails jpg", // For writing album art
            "--write-info-json", // For parsing and writing metadata
        };

        if (settings.SplitChapters)
            args.Add("--split-chapters");

        return string.Join(" ", args.Concat(additionalArgs ?? Enumerable.Empty<string>()));
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
