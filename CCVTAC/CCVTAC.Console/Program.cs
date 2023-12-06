using System.Diagnostics;
using System.Threading;
using CCVTAC.Console.Settings;
using Spectre.Console;

namespace CCVTAC.Console;

internal static class Program
{
    private static readonly string[] _helpCommands = ["-h", "--help"];
    private static readonly string[] _quitCommands = ["q", "quit", "exit", "bye"];
    private const string _inputPrompt = "Enter one or more YouTube resource URLs (or 'q' to quit):";

    static void Main(string[] args)
    {
        Printer printer = new();

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
        UserSettings userSettings = settingsResult.Value;
        SettingsService.PrintSummary(userSettings, printer, "Settings loaded OK.");

        // The working directory should be empty.
        var tempFiles = IoUtilties.Directories.GetDirectoryFiles(userSettings.WorkingDirectory);
        if (tempFiles.Any())
        {
            printer.Error($"{tempFiles.Count} file(s) unexpectedly found in the working directory, so will abort:");
            tempFiles.ForEach(file => printer.Warning($"• {file}"));
            return;
        }

        ResultTracker resultTracker = new(printer);
        History historyLogger = new(userSettings.HistoryLogFilePath);

        while (true)
        {
            NextAction nextAction = ProcessBatch(userSettings, resultTracker, historyLogger, printer);
            if (nextAction != NextAction.Continue)
            {
                break;
            }
        }

        resultTracker.PrintFinalSummary();
    }

    /// <summary>
    /// Processes a single user request, from input to downloading and file post-processing.
    /// </summary>
    /// <param name="userSettings"></param>
    /// <param name="resultHandler"></param>
    /// <param name="printer"></param>
    /// <returns>A bool indicating whether to quit the program (true) or continue (false).</returns>
    private static NextAction ProcessBatch(
        UserSettings userSettings,
        ResultTracker resultHandler,
        History historyLogger,
        Printer printer)
    {
        string userInput = printer.GetInput(_inputPrompt);

        Stopwatch topStopwatch = new();
        topStopwatch.Start();

        var batchUrls = userInput.Split(" ")
                                 .Where(i => i.HasText()) // Remove multiple spaces.
                                 .Distinct()
                                 .ToImmutableList();

        if (batchUrls.Count > 1)
        {
            printer.Print($"Batch of {batchUrls.Count} URLs entered.");
            batchUrls.ForEach(i => printer.Print($"• {i}"));
            printer.PrintEmptyLines(1);
        }

        historyLogger.Append(batchUrls, DateTime.Now, printer);

        nuint currentBatch = 0;
        bool haveProcessedAny = false;

        foreach (string url in batchUrls)
        {
            if (_quitCommands.Contains(url.ToLowerInvariant()))
            {
                return NextAction.QuitAtUserRequest;
            }

            var tempFiles = IoUtilties.Directories.GetDirectoryFiles(userSettings.WorkingDirectory);
            if (tempFiles.Any())
            {
                printer.Error($"{tempFiles.Count} file(s) unexpectedly found in the working directory, so will abort:");
                tempFiles.ForEach(file => printer.Warning($"• {file}"));
                return NextAction.QuitDueToErrors;
            }

            if (haveProcessedAny) // No need to sleep for the very first URL.
            {
                AnsiConsole.Status()
                    .Start($"Sleeping for {userSettings.SleepSecondsBetweenBatches} seconds...", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Star);
                        ctx.SpinnerStyle(Style.Parse("blue"));

                        ushort remainingSeconds = userSettings.SleepSecondsBetweenBatches;
                        while (remainingSeconds > 0)
                        {
                            ctx.Status($"Sleeping for {remainingSeconds} seconds...");
                            remainingSeconds--;
                            Thread.Sleep(1000);
                        }
                        printer.Print($"Slept for {userSettings.SleepSecondsBetweenBatches} second(s).",
                                      appendLines: 1);
                    });
            }
            else
            {
                haveProcessedAny = true;
            }

            if (batchUrls.Count > 1)
                printer.Print($"Processing batch {++currentBatch} of {batchUrls.Count}...");

            Stopwatch jobStopwatch = new();
            jobStopwatch.Start();

            var downloadResult = Downloading.Downloader.Run(url, userSettings, printer);
            resultHandler.RegisterResult(downloadResult);
            if (downloadResult.IsFailed)
            {
                return NextAction.Continue;
            }

            var postProcessor = new PostProcessing.Setup(userSettings, printer);
            postProcessor.Run(); // TODO: Think about if/how to handle leftover temp files due to errors.

            string batchClause = batchUrls.Count > 1
                ? $" (batch {currentBatch} of {batchUrls.Count})"
                : string.Empty;
            // TODO: Use minutes or hours for longer times.
            printer.Print($"Done processing '{url}'{batchClause} in {jobStopwatch.ElapsedMilliseconds:#,##0}ms.");
        }

        if (batchUrls.Count > 1)
        {
            // TODO: Use minutes or hours for longer times.
            printer.Print($"All done with {batchUrls.Count} batches in {topStopwatch.ElapsedMilliseconds:#,##0}ms.");
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
        /// Program execution should end at the user's request.
        /// </summary>
        QuitAtUserRequest,

        /// <summary>
        /// Program execution should end due to an inability to continue.
        /// </summary>
        QuitDueToErrors,
    }
}
