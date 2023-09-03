using CCVTAC.Console.Settings;

namespace CCVTAC.Console;

internal static class Program
{
    private static readonly string[] HelpCommands = new[] { "-h", "--help" };
    private static readonly string[] QuitCommands = new[] { "q", "quit", "exit", "bye" };
    private const string InputPrompt = "Enter a YouTube URL (or 'q' to quit):";

    static void Main(string[] args)
    {
        var printer = new Printer();

        if (args.Length > 0 && HelpCommands.Contains(args[0].ToLowerInvariant()))
        {
            Help.Print(printer);
            return;
        }

        // Catch the user's pressing Ctrl-C (SIGINT).
        System.Console.CancelKeyPress += delegate
        {
            printer.Warning("\nQuitting at user's request.");
        };

        // Top-level `try` to catch and pretty-print unexpected exceptions.
        try
        {
            Start(printer);
        }
        catch (Exception topLevelException)
        {
            printer.Error($"Fatal error: {topLevelException.Message}");
            Spectre.Console.AnsiConsole.WriteException(topLevelException);
        }
    }

    /// <summary>
     /// Does the initial setup and oversees the overall process.
    /// </summary>
    /// <param name="printer"></param>
    private static void Start(Printer printer)
    {
        var settingsResult = SettingsService.GetUserSettings();
        if (settingsResult.IsFailed)
        {
            printer.Errors("Settings file errors:", settingsResult);
            return;
        }
        var userSettings = settingsResult.Value;
        SettingsService.PrintSummary(userSettings, printer, "Settings loaded OK:");

        var resultCounter = new ResultTracker(printer);
        while (true)
        {
            bool shouldQuit = ProcessSingleResource(userSettings, resultCounter, printer);
            if (shouldQuit)
                break;
        }

        resultCounter.PrintFinalSummary();
    }

    /// <summary>
    /// Processes a single user request, from input to downloading and file post-processing.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="resultHandler"></param>
    /// <param name="printer"></param>
    /// <returns>A bool indicating whether to quit the program (true) or continue (false).</returns>
    private static bool ProcessSingleResource(UserSettings settings, ResultTracker resultHandler, Printer printer)
    {
        string userInput = printer.GetInput(InputPrompt);

        if (QuitCommands.Contains(userInput.ToLowerInvariant()))
            return true;

        var mainStopwatch = new System.Diagnostics.Stopwatch();
        mainStopwatch.Start();

        var downloadResult = Downloading.Downloader.Run(userInput, settings, printer);
        resultHandler.RegisterResult(downloadResult);

        if (downloadResult.IsFailed)
            return false;

        History.Append(userInput, printer);

        var postProcessor = new PostProcessing.Setup(settings, printer);
        postProcessor.Run();

        printer.Print($"Done in {mainStopwatch.ElapsedMilliseconds:#,##0}ms.");
        return false;
    }
}
