using CCVTAC.Console.Settings;
using CCVTAC.Console.Downloading;
using System.Threading;
using Spectre.Console;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;
using static CCVTAC.Console.InputHelper;
using CCVTAC.Console.IoUtilties;

namespace CCVTAC.Console;

/// <summary>
/// Drives the primary input gathering and processing tasks.
/// </summary>
internal class Orchestrator
{
    /// <summary>
    /// Ensures the download environment is ready, then initiates the UI input and download process.
    /// </summary>
    internal static void Start(UserSettings settings, Printer printer)
    {
        // Verify the external program for downloading is installed on the system.
        if (Downloader.ExternalTool.ProgramExists() is { IsFailed: true })
        {
            printer.Error(
                $"To use this program, please first install {Downloader.ExternalTool.Name} " +
                $"({Downloader.ExternalTool.Url}) on this system.");
            printer.Print("Pass '--help' for more information.");
            return;
        }

        // The working directory should start empty.
        var emptyDirResult = IoUtilties.Directories.WarnIfAnyFiles(settings.WorkingDirectory, 10);
        if (emptyDirResult.IsFailed)
        {
            printer.FirstError(emptyDirResult);

            var deleteResult = Directories.AskToDeleteAllFiles(settings.WorkingDirectory, printer);
            if (deleteResult.IsSuccess)
            {
                printer.Print($"{deleteResult.Value} file(s) deleted.");
            }
            else
            {
                printer.FirstError(deleteResult);
                printer.Print($"Aborting...");
                return;
            }
        }

        var resultTracker = new ResultTracker(printer);
        var history = new History(settings.HistoryFile, settings.HistoryDisplayCount);

        while (true)
        {
            var input = printer.GetInput(InputHelper.Prompt);
            var splitInputs = InputHelper.SplitInput(input);

            if (splitInputs.IsEmpty)
            {
                printer.Error($"Invalid input. Enter only URLs or commands beginning with \"{Commands.Prefix}\".");
                continue;
            }

            var categorizedInputs = InputHelper.CategorizeInputs(splitInputs);

            int urlCount = categorizedInputs.Count(i => i.InputType == InputType.Url);
            SummarizeInput(categorizedInputs, urlCount, printer);

            var nextAction = ProcessBatch(categorizedInputs, ref settings, resultTracker, history, urlCount, printer);
            if (nextAction is not NextAction.Continue)
            {
                break;
            }
        }

        resultTracker.PrintSummary();
    }

    /// <summary>
    /// Processes a single user request, from input to downloading and file post-processing.
    /// </summary>
    /// <returns>Returns the next action the application should take (e.g., continue or quit).</returns>
    private static NextAction ProcessBatch(
        ImmutableArray<CategorizedInput> categorizedInputs,
        ref UserSettings settings,
        ResultTracker resultTracker,
        History history,
        int urlCount,
        Printer printer)
    {
        var inputTime = DateTime.Now;
        Watch watch = new();

        nuint currentBatch = 0;

        foreach (CategorizedInput input in categorizedInputs)
        {
            var result = input.InputType is InputType.Command
                ? ProcessCommand(input.Text, ref settings, history, printer)
                : ProcessUrl(input.Text, settings, resultTracker, history, printer, inputTime, urlCount, ++currentBatch);

            if (result.IsFailed)
            {
                printer.Error(result.Errors[0].Message);
                continue;
            }

            NextAction next = result.Value;
            if (next is NextAction.QuitAtUserRequest)
            {
                return next;
            }
        }

        if (urlCount > 1)
        {
            printer.Print($"{Environment.NewLine}Finished with {urlCount} batches in {watch.ElapsedFriendly}.");
        }

        return NextAction.Continue;
    }

    private static NextAction ProcessUrl(
        string url,
        UserSettings settings,
        ResultTracker resultTracker,
        History history,
        Printer printer,
        DateTime inputTime,
        int urlCount,
        nuint currentBatch)
    {
        var emptyDirResult = IoUtilties.Directories.WarnIfAnyFiles(settings.WorkingDirectory, 10);
        if (emptyDirResult.IsFailed)
        {
            // TODO: Create a command to clear temporary files.
            printer.FirstError(emptyDirResult);
            return NextAction.QuitDueToErrors;
        }

        if (currentBatch > 1) // Don't sleep for the very first URL.
        {
            Sleep(settings.SleepSecondsBetweenBatches);
            printer.Print($"Slept for {settings.SleepSecondsBetweenBatches} second(s).", appendLines: 1);
        }

        if (urlCount > 1)
        {
            printer.Print($"Processing batch {currentBatch} of {urlCount}...");
        }

        Watch jobWatch = new();

        var mediaTypeResult = Downloader.GetMediaType(url);
        if (mediaTypeResult.IsFailed)
        {
            printer.Error($"Error parsing URL {url}: {mediaTypeResult.Errors.First().Message}");
            return NextAction.Continue;
        }
        var mediaType = mediaTypeResult.Value;
        printer.Print($"{mediaType.GetType().Name} URL '{url}' detected.");

        history.Append(url, inputTime, settings.VerboseOutput, printer);

        var downloadResult = Downloader.Run(url, mediaType, settings, printer);
        resultTracker.RegisterResult(downloadResult);
        if (downloadResult.IsFailed)
        {
            return NextAction.Continue;
        }

        var postProcessor = new PostProcessing.PostProcessing(settings, mediaType, printer);
        postProcessor.Run();

        string batchClause = urlCount > 1
            ? $" (batch {currentBatch} of {urlCount})"
            : string.Empty;

        printer.Print($"Processed '{url}'{batchClause} in {jobWatch.ElapsedFriendly}.");
        return NextAction.Continue;
    }

    private static Result<NextAction> ProcessCommand(
        string command,
        ref UserSettings settings,
        History history,
        Printer printer)
    {
        if (Commands._quit.CaseInsensitiveContains(command))
        {
            return Result.Ok(NextAction.QuitAtUserRequest);
        }

        if (Commands._history.CaseInsensitiveContains(command))
        {
            history.ShowRecent(printer);
            return Result.Ok(NextAction.Continue);
        }

        if (Commands._showSettings.CaseInsensitiveContains(command))
        {
            SettingsAdapter.PrintSummary(settings, printer);
            return Result.Ok(NextAction.Continue);
        }

        if (Commands._toggleSplitChapter.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleSplitChapters(settings);
            SettingsAdapter.PrintSummary(
                settings, printer, "Split Chapters was toggled for this session.");
            return Result.Ok(NextAction.Continue);
        }

        if (Commands._toggleEmbedImages.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleEmbedImages(settings);
            SettingsAdapter.PrintSummary(
                settings, printer, "Embed Images was toggled for this session.");
            return Result.Ok(NextAction.Continue);
        }

        if (Commands._toggleVerboseOutput.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleVerboseOutput(settings);
            SettingsAdapter.PrintSummary(
                settings, printer, "Verbose Output was toggled for this session.");
            return Result.Ok(NextAction.Continue);
        }

        return Result.Fail($"\"{command}\" is not a valid command.");
    }

    private static void SummarizeInput(
        ImmutableArray<CategorizedInput> categorizedInputs,
        int urlCount,
        Printer printer)
    {
        var commandCount = categorizedInputs.Length - urlCount;

        if (categorizedInputs.Length > 1)
        {
            var urlSummary = urlCount switch
            {
                1 => "1 URL",
                >1 => $"{urlCount} URLs",
                _ => string.Empty
            };
            var commandSummary = commandCount switch
            {
                1 => "1 command",
                >1 => $"{commandCount} commands",
                _ => string.Empty
            };
            var connector = urlSummary.HasText() && commandSummary.HasText()
                ? " and "
                : string.Empty;
            printer.Print($"Batch of {urlSummary}{connector}{commandSummary} entered.");

            foreach (CategorizedInput input in categorizedInputs)
            {
                printer.Print($" • {input.Text}");
            }
            printer.PrintEmptyLines(1);
        }
    }

    private static void Sleep(ushort sleepSeconds)
    {
        ushort remainingSeconds = sleepSeconds;

        AnsiConsole.Status()
            .Start($"Sleeping for {sleepSeconds} seconds...", ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("blue"));

                while (remainingSeconds > 0)
                {
                    ctx.Status($"Sleeping for {remainingSeconds} seconds...");
                    remainingSeconds--;
                    Thread.Sleep(1000);
                }
            });
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
