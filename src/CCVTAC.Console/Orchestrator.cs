using CCVTAC.Console.Settings;
using CCVTAC.Console.Downloading;
using CCVTAC.Console.IoUtilties;
using CCVTAC.Console.PostProcessing;
using Spectre.Console;
using System.Threading;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;
using static CCVTAC.Console.InputHelper;

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
            printer.Info("Pass '--help' for more information.");
            return;
        }

        // The working directory should start empty. Give the user a chance to empty it.
        var emptyDirResult = IoUtilties.Directories.WarnIfAnyFiles(settings.WorkingDirectory, 10);
        if (emptyDirResult.IsFailed)
        {
            printer.FirstError(emptyDirResult);

            var deleteResult = Directories.AskToDeleteAllFiles(settings.WorkingDirectory, printer);
            if (deleteResult.IsSuccess)
            {
                printer.Info($"{deleteResult.Value} file(s) deleted.");
            }
            else
            {
                printer.FirstError(deleteResult);
                printer.Info($"Aborting...");
                return;
            }
        }

        var resultTracker = new ResultTracker(printer);
        var history = new History(settings.HistoryFile, settings.HistoryDisplayCount);
        var nextAction = NextAction.Continue;

        while (nextAction is NextAction.Continue)
        {
            var input = printer.GetInput(InputHelper.Prompt);
            var splitInputs = InputHelper.SplitInput(input);

            if (splitInputs.IsEmpty)
            {
                printer.Error($"Invalid input. Enter only URLs or commands beginning with \"{Commands.Prefix}\".");
                continue;
            }

            var categorizedInputs = InputHelper.CategorizeInputs(splitInputs);
            var categoryCounts = InputHelper.CountCategories(categorizedInputs);

            SummarizeInput(categorizedInputs, categoryCounts, printer);

            nextAction = ProcessBatch(
                categorizedInputs, categoryCounts, ref settings,
                resultTracker, history, printer);
        }

        resultTracker.PrintSummary();
    }

    /// <summary>
    /// Processes a single user request, from input to downloading and file post-processing.
    /// </summary>
    /// <returns>Returns the next action the application should take (e.g., continue or quit).</returns>
    private static NextAction ProcessBatch(
        ImmutableArray<CategorizedInput> categorizedInputs,
        CategoryCounts categoryCounts,
        ref UserSettings settings,
        ResultTracker resultTracker,
        History history,
        Printer printer)
    {
        var inputTime = DateTime.Now;
        Watch watch = new();

        nuint currentBatch = 0;

        foreach (CategorizedInput input in categorizedInputs)
        {
            var result = input.Category is InputCategory.Command
                ? ProcessCommand(input.Text, ref settings, history, printer)
                : ProcessUrl(input.Text, settings, resultTracker, history, inputTime,
                             categoryCounts[InputCategory.Url], ++currentBatch, printer);

            if (result.IsFailed)
            {
                printer.FirstError(result);
                continue;
            }

            NextAction next = result.Value;
            if (next is NextAction.QuitAtUserRequest)
            {
                return next;
            }
        }

        if (categoryCounts[InputCategory.Url] > 1)
        {
            printer.Info($"{Environment.NewLine}Finished with {categoryCounts[InputCategory.Url]} batches in {watch.ElapsedFriendly}.");
        }

        return NextAction.Continue;
    }

    private static NextAction ProcessUrl(
        string url,
        UserSettings settings,
        ResultTracker resultTracker,
        History history,
        DateTime urlInputTime,
        int urlCount,
        nuint currentBatch,
        Printer printer)
    {
        var emptyDirResult = IoUtilties.Directories.WarnIfAnyFiles(settings.WorkingDirectory, 10);
        if (emptyDirResult.IsFailed)
        {
            printer.FirstError(emptyDirResult);
            return NextAction.QuitDueToErrors;
        }

        if (currentBatch > 1) // Don't sleep for the very first URL.
        {
            Sleep(settings.SleepSecondsBetweenBatches);
            printer.Info($"Slept for {settings.SleepSecondsBetweenBatches} second(s).", appendLines: 1);
        }

        if (urlCount > 1)
        {
            printer.Info($"Processing batch {currentBatch} of {urlCount}...");
        }

        Watch jobWatch = new();

        var mediaTypeResult = Downloader.GetMediaType(url);
        if (mediaTypeResult.IsFailed)
        {
            printer.Error($"Error parsing URL {url}: {mediaTypeResult.Errors.First().Message}");
            return NextAction.Continue;
        }
        var mediaType = mediaTypeResult.Value;
        printer.Info($"{mediaType.GetType().Name} URL '{url}' detected.");

        history.Append(url, urlInputTime, printer);

        var downloadResult = Downloader.Run(url, mediaType, settings, printer);
        resultTracker.RegisterResult(downloadResult);
        if (downloadResult.IsFailed)
        {
            return NextAction.Continue;
        }

        PostProcessor.Run(settings, mediaType, printer);

        string batchClause = urlCount > 1
            ? $" (batch {currentBatch} of {urlCount})"
            : string.Empty;

        printer.Info($"Processed '{url}'{batchClause} in {jobWatch.ElapsedFriendly}.");
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

        static string SummarizeToggle(string settingName, bool setting)
            => $"{settingName} was toggled for this session and is now {(setting ? "ON" : "OFF")}.";

        if (Commands._toggleSplitChapter.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleSplitChapters(settings);
            printer.Info(SummarizeToggle("Split Chapters", settings.SplitChapters));
            return Result.Ok(NextAction.Continue);
        }

        if (Commands._toggleEmbedImages.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleEmbedImages(settings);
            printer.Info(SummarizeToggle("Embed Images", settings.EmbedImages));
            return Result.Ok(NextAction.Continue);
        }

        if (Commands._toggleQuietMode.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleQuietMode(settings);
            printer.Info(SummarizeToggle("Quiet Mode", settings.QuietMode));
            printer.ShowDebug(!settings.QuietMode);
            return Result.Ok(NextAction.Continue);
        }

        return Result.Fail($"\"{command}\" is not a valid command.");
    }

    private static void SummarizeInput(
        ImmutableArray<CategorizedInput> categorizedInputs,
        CategoryCounts counts,
        Printer printer)
    {
        if (categorizedInputs.Length > 1)
        {
            var urlSummary = counts[InputCategory.Url] switch
            {
                1 => "1 URL",
                >1 => $"{counts[InputCategory.Url]} URLs",
                _ => string.Empty
            };

            var commandSummary = counts[InputCategory.Command] switch
            {
                1 => "1 command",
                >1 => $"{counts[InputCategory.Command]} commands",
                _ => string.Empty
            };

            var connector = urlSummary.HasText() && commandSummary.HasText()
                ? " and "
                : string.Empty;

            printer.Info($"Batch of {urlSummary}{connector}{commandSummary} entered.");

            foreach (CategorizedInput input in categorizedInputs)
            {
                printer.Info($" • {input.Text}");
            }
            printer.EmptyLines(1);
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
