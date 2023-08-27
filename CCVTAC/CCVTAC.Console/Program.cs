using CCVTAC.Console.Settings;

namespace CCVTAC.Console;

internal static class Program
{
    private static readonly string[] HelpCommands = new[] { "-h", "--help" };
    private static readonly string[] QuitCommands = new[] { "q", "quit", "exit", "bye" };
    private static readonly string InputPrompt = "Enter a YouTube URL (or 'q' to quit):";

    static void Main(string[] args)
    {
        var printer = new Printer();

        if (args.Length > 0 && HelpCommands.Contains(args[0].ToLowerInvariant()))
        {
            Help.Print(printer);
            return;
        }

        // Top-level `try` to catch and pretty-print unexpected exceptions.
        try
        {
            var settingsResult = GetSettings();
            if (settingsResult.IsFailed)
            {
                printer.Errors("Settings file errors:", settingsResult);
                return;
            }
            var settings = settingsResult.Value;
            SettingsService.PrintSummary(settings, printer, "Settings loaded OK:");

            TagFormat.SetId3v2Version(
                version: TagFormat.Id3v2Version.TwoPoint3,
                forceAsDefault: true);

            var resultCounter = new ResultHandler(printer);
            while (true)
            {
                if (!Run(settings, resultCounter, printer))
                    break;
            }

            resultCounter.PrintFinalSummary();
        }
        catch (Exception ex)
        {
            printer.Error($"Fatal error: {ex.Message}");
            Spectre.Console.AnsiConsole.WriteException(ex);
        }
    }

    private static bool Run(UserSettings settings, ResultHandler resultHandler, Printer printer)
    {
        string userInput = printer.GetInput(InputPrompt);

        if (QuitCommands.Contains(userInput.ToLowerInvariant()))
        {
            return false;
        }

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var downloadResult = Downloading.Downloader.Run(userInput, settings, printer);
        resultHandler.RegisterResult(downloadResult);

        History.Append(userInput, printer);

        var postProcessor = new PostProcessing.Setup(settings, printer);
        postProcessor.Run();

        printer.Print($"Done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
        return true;
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
            return Result.Fail(ensureValidSettingsResult.Errors.Select(e => e.Message));
        }

        return readSettingsResult.Value;
    }
}
