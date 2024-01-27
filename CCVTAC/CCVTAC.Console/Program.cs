using System.Threading;
using CCVTAC.Console.Settings;
using Spectre.Console;

namespace CCVTAC.Console;

internal static class Program
{
    private static readonly string[] _helpFlags = ["-h", "--help"];
    private static readonly string[] _historyCommands = ["--history", "history"];
    private static readonly string[] _settingsFileCommands = ["-s", "--settings"];
    private static readonly string[] _quitCommands = ["q", "quit", "exit", "bye"];
    private const string _urlInputPrompt = "Enter one or more YouTube media URLs, 'quit', or 'history':";

    static void Main(string[] args)
    {
        Printer printer = new();

        if (args.Length > 0 && _helpFlags.Contains(args[0].ToLowerInvariant()))
        {
            Help.Print(printer);
            return;
        }

        string? maybeSettingsPath = args.Length >= 2 && _settingsFileCommands.Contains(args[0].ToLowerInvariant())
                ? args[1] // Expected to be a settings file path.
                : null;
        SettingsService settingsService = new(maybeSettingsPath);

        var settingsResult = settingsService.PrepareUserSettings();
        if (settingsResult.IsFailed)
        {
            printer.Errors("Settings error(s):", settingsResult);
            return;
        }
        UserSettings userSettings = settingsResult.Value;
        settingsService.PrintSummary(userSettings, printer, header: "Settings loaded OK.");

        History history = new(userSettings.HistoryFilePath, userSettings.HistoryDisplayCount);

        // Show the history if requested.
        if (args.Length > 0 && _historyCommands.Contains(args[0].ToLowerInvariant()))
        {
            history.ShowRecent(printer);
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
            Start(userSettings, history, printer);
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
    private static void Start(UserSettings userSettings, History history, Printer printer)
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

        // The working directory should be empty.
        var tempFiles = IoUtilties.Directories.GetDirectoryFiles(userSettings.WorkingDirectory);
        if (tempFiles.Any())
        {
            printer.Error($"{tempFiles.Count} file(s) unexpectedly found in the working directory, so will abort:");
            tempFiles.ForEach(file => printer.Warning($"• {file}"));
            return;
        }

        ResultTracker resultTracker = new(printer);

        while (true)
        {
            NextAction nextAction = ProcessBatch(userSettings, resultTracker, history, printer);
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
        History history,
        Printer printer)
    {
        string userInput = printer.GetInput(_urlInputPrompt);
        DateTime inputTime = DateTime.Now;

        Watch watch = new();

        var batchUrls = userInput.Split(" ")
                                 .Where(i => i.HasText())
                                 .Distinct()
                                 .ToImmutableList();

        if (batchUrls.Count > 1)
        {
            printer.Print($"Batch of {batchUrls.Count} URLs entered.");
            batchUrls.ForEach(i => printer.Print($"• {i}"));
            printer.PrintEmptyLines(1);
        }

        nuint currentBatch = 0;
        bool haveProcessedAny = false;

        foreach (string url in batchUrls)
        {
            if (_quitCommands.Contains(url.ToLowerInvariant()))
            {
                return NextAction.QuitAtUserRequest;
            }

            // Show the history if requested.
            if (_historyCommands.Contains(url.ToLowerInvariant()))
            {
                history.ShowRecent(printer);
                continue;
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

            Watch jobWatch = new();

            history.Append(url, inputTime, printer);

            var downloadResult = Downloading.Downloader.Run(url, userSettings, printer);
            resultHandler.RegisterResult(downloadResult);
            if (downloadResult.IsFailed)
            {
                return NextAction.Continue;
            }

            history.Append(url, inputTime, printer);

            var postProcessor = new PostProcessing.Setup(userSettings, printer);
            postProcessor.Run(); // TODO: Think about if/how to handle leftover temp files due to errors.

            string batchClause = batchUrls.Count > 1
                ? $" (batch {currentBatch} of {batchUrls.Count})"
                : string.Empty;
            printer.Print($"Done processing '{url}'{batchClause} in {jobWatch.ElapsedFriendly}.");
        }

        if (batchUrls.Count > 1)
        {
            printer.Print($"All done with {batchUrls.Count} batches in {watch.ElapsedFriendly}.");
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
