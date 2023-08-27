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
            var settingsResult = SettingsService.GetSettings();
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
        catch (Exception topLevelException)
        {
            printer.Error($"Fatal error: {topLevelException.Message}");
            Spectre.Console.AnsiConsole.WriteException(topLevelException);
        }
    }

    /// <summary>
    /// Processes a single user request, from input to downloading and file post-processing.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="resultHandler"></param>
    /// <param name="printer"></param>
    /// <returns>A bool indicating whether to continue (true) or quit the program (false).</returns>
    private static bool Run(UserSettings settings, ResultHandler resultHandler, Printer printer)
    {
        string userInput = printer.GetInput(InputPrompt);

        if (QuitCommands.Contains(userInput.ToLowerInvariant()))
        {
            return false;
        }

        var mainStopwatch = new System.Diagnostics.Stopwatch();
        mainStopwatch.Start();

        var downloadResult = Downloading.Downloader.Run(userInput, settings, printer);
        resultHandler.RegisterResult(downloadResult);

        History.Append(userInput, printer);

        var postProcessor = new PostProcessing.Setup(settings, printer);
        postProcessor.Run();

        printer.Print($"Done in {mainStopwatch.ElapsedMilliseconds:#,##0}ms.");
        return true;
    }
}
