using System.Threading;
using CCVTAC.Console.Settings;
using Spectre.Console;

namespace CCVTAC.Console;

internal static class Program
{
    private static readonly string[] _helpCommands = new[] { "-h", "--help" };
    private static readonly string[] _quitCommands = new[] { "q", "quit", "exit", "bye" };
    private const string _inputPrompt = "Enter one or more YouTube resource URLs (or 'q' to quit):";

    static void Main(string[] args)
    {
        var printer = new Printer();

        if (args.Length > 0 && _helpCommands.Contains(args[0].ToLowerInvariant()))
        {
            Help.Print(printer);
            return;
        }

        // Catch the user's pressing Ctrl-C (SIGINT).
        System.Console.CancelKeyPress += delegate
        {
            printer.Warning("\nQuitting at user's request.");
        };

        // Top-level `try` block to catch and pretty-print unexpected exceptions.
        try
        {
            Start(printer);
        }
        catch (Exception topException)
        {
            printer.Error($"Fatal error: {topException.Message}");
            AnsiConsole.WriteException(topException);
            printer.Print("Please help improve this tool by reporting this error and the URL you entered at https://github.com/codeconscious/ccvtac/issues.");
        }
    }

    /// <summary>
     /// Performs initial setup, initiates each download request, and prints the final summary when the user requests to end the program.
    /// </summary>
    private static void Start(Printer printer)
    {
        // Verify the external program for downloading is installed on the system.
        if (Downloading.Downloader.ExternalProgram.ProgramExists() is { IsFailed: true })
        {
            printer.Error(
                $"To use this program, please first install {Downloading.Downloader.ExternalProgram.Name} " +
                $"({Downloading.Downloader.ExternalProgram.Url}) on this system.");
            printer.Print("Pass '--help' to this program for more information.");
            return;
        }

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
            var nextAction = ProcessSingleInput(userSettings, resultCounter, printer);
            if (nextAction == NextAction.Quit)
            {
                break;
            }
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
    private static NextAction ProcessSingleInput(UserSettings settings, ResultTracker resultHandler, Printer printer)
    {
        string userInput = printer.GetInput(_inputPrompt);

        var mainStopwatch = new System.Diagnostics.Stopwatch();
        mainStopwatch.Start();

        var splitInput = userInput.Split(" ")
                                  .Where(i => !string.IsNullOrWhiteSpace(i)) // Handle multiple spaces.
                                  .Distinct()
                                  .ToImmutableList();

        if (splitInput.Count > 1)
        {
            printer.Print($"Batch of {splitInput.Count} URLs entered.");
            splitInput.ForEach(i => printer.Print($"• {i}"));
            printer.PrintEmptyLines(1);
        }

        var haveProcessedAny = false;

        foreach (var input in splitInput)
        {
            if (_quitCommands.Contains(input.ToLowerInvariant()))
            {
                return NextAction.Quit;
            }

            if (haveProcessedAny) // No need to sleep for the very first URL.
            {
                AnsiConsole.Status()
                    .Start($"Sleeping for {settings.SleepBetweenBatchesSeconds} seconds...", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Star);
                        ctx.SpinnerStyle(Style.Parse("blue"));

                        var remainingSeconds = settings.SleepBetweenBatchesSeconds;
                        while (remainingSeconds > 0)
                        {
                            ctx.Status($"Sleeping for {remainingSeconds} seconds...");
                            remainingSeconds--;
                            Thread.Sleep(1000);
                        }
                        printer.Print($"Slept for {settings.SleepBetweenBatchesSeconds} second(s).",
                                      appendLines: 1);
                    });
            }
            else
            {
                haveProcessedAny = true;
            }

            var jobStopwatch = new System.Diagnostics.Stopwatch();
            jobStopwatch.Start();

            var downloadResult = Downloading.Downloader.Run(input, settings, printer);
            resultHandler.RegisterResult(downloadResult);

            if (downloadResult.IsFailed)
            {
                return NextAction.Continue;
            }

            History.Append(input, printer);

            var postProcessor = new PostProcessing.Setup(settings, printer);
            postProcessor.Run(); // TODO: Think about if/how to handle leftover temp files due to errors.

            // TODO: Use minutes or hours for longer times.
            printer.Print($"Done processing '{input}' in {jobStopwatch.ElapsedMilliseconds:#,##0}ms.",
                          appendLines: 1);
        }

        if (splitInput.Count > 1)
        {
            printer.Print($"All done in {mainStopwatch.ElapsedMilliseconds:#,##0}ms.");
        }

        return NextAction.Continue;
    }

    /// <summary>
    /// Actions that inform the program what it should do after the current step is done.
    /// </summary>
    private enum NextAction : byte
    {
        /// <summary>
        /// Program execution should continue and not end.
        /// </summary>
        Continue,

        /// <summary>
        /// Program execution should end.
        /// </summary>
        Quit
    }
}
