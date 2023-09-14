using CCVTAC.Console.Settings;

namespace CCVTAC.Console;

internal static class Program
{
    private static readonly string[] HelpCommands = new[] { "-h", "--help" };
    private static readonly string[] QuitCommands = new[] { "q", "quit", "exit", "bye" };
    private const string InputPrompt = "Enter one or more YouTube resource URLs separated by spaces (or 'q' to quit):";

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
            bool shouldQuit = ProcessSingleInput(userSettings, resultCounter, printer);
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
    private static bool ProcessSingleInput(UserSettings settings, ResultTracker resultHandler, Printer printer)
    {
        string userInput = printer.GetInput(InputPrompt);

        var mainStopwatch = new System.Diagnostics.Stopwatch();
        mainStopwatch.Start();

        var splitInput = userInput.Split(" ")
                                  .Where(i => !string.IsNullOrWhiteSpace(i))
                                  .ToImmutableArray();

        var haveProcessedAny = false;

        foreach (var input in splitInput)
        {
            if (QuitCommands.Contains(input.ToLowerInvariant()))
                return true;

            if (haveProcessedAny) // No need to sleep for the very first URL.
            {
                var sleepSeconds = settings.SleepBetweenBatchesSeconds;
                printer.Print($"Sleeping for {sleepSeconds} seconds...", appendLines: 1);
                System.Threading.Thread.Sleep(sleepSeconds * 1000);
            }
            else
            {
                haveProcessedAny = true;
            }

            var thisStopwatch = new System.Diagnostics.Stopwatch();
            thisStopwatch.Start();

            var downloadResult = Downloading.Downloader.Run(input, settings, printer);
            resultHandler.RegisterResult(downloadResult);

            if (downloadResult.IsFailed)
                return false;

            History.Append(input, printer);

            var postProcessor = new PostProcessing.Setup(settings, printer);
            postProcessor.Run(); // TODO: Think about if/how to handle leftover temp files.

            printer.Print($"Done processing `{input}` in {thisStopwatch.ElapsedMilliseconds:#,##0}ms.", appendLines: 1);
        }

        if (splitInput.Length > 1)
            printer.Print($"All done in {mainStopwatch.ElapsedMilliseconds:#,##0}ms.");

        return false;
    }
}
