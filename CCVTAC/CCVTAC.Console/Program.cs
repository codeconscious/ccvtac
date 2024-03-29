﻿using System.Threading;
using CCVTAC.Console.Settings;
using Spectre.Console;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console;

internal static class Program
{
    private static readonly string[] _helpFlags = ["-h", "--help"];
    private static readonly string[] _historyCommands = ["--history", "history"];
    private static readonly string[] _settingsFileCommands = ["-s", "--settings"];
    private static readonly string[] _quitCommands = ["q", "quit", "exit", "bye"];
    private const string _urlPrompt =
        "Enter one or more YouTube media URLs (separated by spaces), 'history', or 'quit':\n▶︎";

    private const string _defaultSettingsFileName = "settings.json";

    static void Main(string[] args)
    {
        Printer printer = new();

        if (args.Length > 0 && _helpFlags.Contains(args[0].ToLowerInvariant()))
        {
            Help.Print(printer);
            return;
        }

        string? maybeSettingsPath = args.Length >= 2 &&
                                    _settingsFileCommands.Contains(args[0].ToLowerInvariant())
                ? args[1] // Expected to be a settings file path.
                : _defaultSettingsFileName;

        UserSettings settings;
        var result = SettingsAdapter.ProcessSettings(maybeSettingsPath, printer);
        if (result.IsFailed)
        {
            printer.Errors(result.Errors.Select(e => e.Message));
            return;
        }
        else if (result.Value is null)
        {
            return;
        }
        else
        {
            settings = result.Value;
        }
        SettingsAdapter.PrintSummary(settings, printer, header: "Settings loaded OK.");

        History history = new(settings.HistoryFile, settings.HistoryDisplayCount);

        // Show the history if requested.
        if (args.Length > 0 && _historyCommands.Contains(args[0].ToLowerInvariant()))
        {
            history.ShowRecent(printer);
            return;
        }

        // Catch the user's pressing Ctrl-C (SIGINT).
        System.Console.CancelKeyPress += delegate
        {
            printer.Warning("\nQuitting at user's request. You might want to verify and delete the files in the working directory.");
            printer.Warning($"Working directory: {settings.WorkingDirectory}");
        };

        // Top-level `try` block to catch and pretty-print unexpected exceptions.
        try
        {
            Start(settings, history, printer);
        }
        catch (Exception topException)
        {
            printer.Error($"Fatal error: {topException.Message}");
            AnsiConsole.WriteException(topException);
            printer.Print("Please help improve this tool by reporting this error and any URLs you entered at https://github.com/codeconscious/ccvtac/issues.");
        }
    }

    /// <summary>
     /// Performs initial setup, initiates each download request, and prints the final summary when the user requests to end the program.
    /// </summary>
    private static void Start(UserSettings settings, History history, Printer printer)
    {
        // Verify the external program for downloading is installed on the system.
        if (Downloading.Downloader.ExternalTool.ProgramExists() is { IsFailed: true })
        {
            printer.Error(
                $"To use this program, please first install {Downloading.Downloader.ExternalTool.Name} " +
                $"({Downloading.Downloader.ExternalTool.Url}) on this system.");
            printer.Print("Pass '--help' to this program for more information.");
            return;
        }

        // The working directory should start empty.
        var tempFiles = IoUtilties.Directories.GetDirectoryFiles(settings.WorkingDirectory);
        if (tempFiles.Any())
        {
            printer.Error($"Aborting due to {tempFiles.Count} file(s) unexpectedly found in the working directory ({settings.WorkingDirectory}):");
            tempFiles.ForEach(file => printer.Warning($"• {file}"));
            return;
        }

        ResultTracker resultTracker = new(printer);

        while (true)
        {
            var nextAction = ProcessBatch(settings, resultTracker, history, printer);
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
    /// <returns>The appropriate next action the application should take.</returns>
    private static NextAction ProcessBatch(
        UserSettings settings,
        ResultTracker resultHandler,
        History history,
        Printer printer)
    {
        string userInput = printer.GetInput(_urlPrompt);
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

            var tempFiles = IoUtilties.Directories.GetDirectoryFiles(settings.WorkingDirectory);
            if (tempFiles.Any())
            {
                printer.Error($"{tempFiles.Count} file(s) unexpectedly found in the working directory ({settings.WorkingDirectory}), so will abort:");
                tempFiles.ForEach(file => printer.Warning($"• {file}"));
                return NextAction.QuitDueToErrors;
            }

            if (haveProcessedAny) // No need to sleep for the very first URL.
            {
                AnsiConsole.Status()
                    .Start($"Sleeping for {settings.SleepSecondsBetweenBatches} seconds...", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Star);
                        ctx.SpinnerStyle(Style.Parse("blue"));

                        ushort remainingSeconds = settings.SleepSecondsBetweenBatches;
                        while (remainingSeconds > 0)
                        {
                            ctx.Status($"Sleeping for {remainingSeconds} seconds...");
                            remainingSeconds--;
                            Thread.Sleep(1000);
                        }
                        printer.Print($"Slept for {settings.SleepSecondsBetweenBatches} second(s).",
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

            var downloadResult = Downloading.Downloader.Run(url, settings, printer);
            resultHandler.RegisterResult(downloadResult);
            if (downloadResult.IsFailed)
            {
                return NextAction.Continue;
            }

            var postProcessor = new PostProcessing.Setup(settings, printer);
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
