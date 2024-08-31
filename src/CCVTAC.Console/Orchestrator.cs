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

        var resultTracker = new ResultTracker<string>(printer);
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

        resultTracker.PrintSessionSummary();
    }

    /// <summary>
    /// Processes a single user request, from input to downloading and file post-processing.
    /// </summary>
    /// <returns>Returns the next action the application should take (e.g., continue or quit).</returns>
    private static NextAction ProcessBatch(
        ImmutableArray<CategorizedInput> categorizedInputs,
        CategoryCounts categoryCounts,
        ref UserSettings settings,
        ResultTracker<string> resultTracker,
        History history,
        Printer printer)
    {
        var inputTime = DateTime.Now;
        var nextAction = NextAction.Continue;
        Watch watch = new();

        int currentBatch = 0;
        ResultTracker<NextAction> batchResultTracker = new(printer);

        foreach (CategorizedInput input in categorizedInputs)
        {
            var result = input.Category is InputCategory.Command
                ? ProcessCommand(input.Text, ref settings, history, printer)
                : ProcessUrl(input.Text, settings, resultTracker, history, inputTime,
                             categoryCounts[InputCategory.Url], ++currentBatch, printer);

            batchResultTracker.RegisterResult(input.Text, result);

            if (result.IsFailed)
            {
                continue;
            }

            nextAction = result.Value;

            if (nextAction is not NextAction.Continue)
            {
                break;
            }
        }

        if (categoryCounts[InputCategory.Url] > 1)
        {
            printer.Info($"{Environment.NewLine}Finished with {categoryCounts[InputCategory.Url]} batches in {watch.ElapsedFriendly}.");
            batchResultTracker.PrintBatchFailures();
        }

        return nextAction;
    }

    private static Result<NextAction> ProcessUrl(
        string url,
        UserSettings settings,
        ResultTracker<string> resultTracker,
        History history,
        DateTime urlInputTime,
        int batchSize,
        int currentBatch,
        Printer printer)
    {
        var emptyDirResult = IoUtilties.Directories.WarnIfAnyFiles(settings.WorkingDirectory, 10);
        if (emptyDirResult.IsFailed)
        {
            printer.FirstError(emptyDirResult);
            return NextAction.QuitDueToErrors; // TODO: Perhaps determine a better way.
        }

        if (currentBatch > 1) // Don't sleep for the very first URL.
        {
            Sleep(settings.SleepSecondsBetweenBatches);
            printer.Info($"Slept for {settings.SleepSecondsBetweenBatches} second(s).", appendLines: 1);
        }

        if (batchSize > 1)
        {
            printer.Info($"Processing batch {currentBatch} of {batchSize}...");
        }

        Watch jobWatch = new();

        var mediaTypeResult = Downloader.GetMediaType(url);
        if (mediaTypeResult.IsFailed)
        {
            var errorMsg = $"URL parse error: {mediaTypeResult.Errors.First().Message}";
            printer.Error(errorMsg);
            return Result.Fail(errorMsg);
        }
        var mediaType = mediaTypeResult.Value;

        printer.Info($"{mediaType.GetType().Name} URL '{url}' detected.");
        history.Append(url, urlInputTime, printer);

        var downloadResult = Downloader.Run(mediaType, settings, printer);
        resultTracker.RegisterResult(url, downloadResult);

        if (downloadResult.IsFailed)
        {
            var errorMsg = $"Download error: {downloadResult.Errors.First().Message}";
            printer.Error(errorMsg);
            return Result.Fail(errorMsg);
        }

        PostProcessor.Run(settings, mediaType, printer);

        string batchClause = batchSize > 1
            ? $" (batch {currentBatch} of {batchSize})"
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
        if (Commands.SummaryCommand.Equals(command, StringComparison.InvariantCultureIgnoreCase))
        {
            Table table = new();
            table.Border(TableBorder.Simple);
            table.AddColumns("Command", "Description");
            table.HideHeaders();
            table.Columns[0].PadRight(3);

            foreach (var (cmd, description) in Commands.Summary)
            {
                table.AddRow(cmd, description);
            }

            printer.PrintTable(table);
            return Result.Ok(NextAction.Continue);
        }

        if (Commands.QuitOptions.CaseInsensitiveContains(command))
        {
            return Result.Ok(NextAction.QuitAtUserRequest);
        }

        if (Commands.History.CaseInsensitiveContains(command))
        {
            history.ShowRecent(printer);
            return Result.Ok(NextAction.Continue);
        }

        if (Commands.SettingsSummary.CaseInsensitiveContains(command))
        {
            SettingsAdapter.PrintSummary(settings, printer);
            return Result.Ok(NextAction.Continue);
        }

        static string SummarizeToggle(string settingName, bool setting)
            => $"{settingName} was toggled to {(setting ? "ON" : "OFF")} for this session.";

        static string SummarizeUpdate(string settingName, string setting)
            => $"{settingName} was updated to \"{setting}\" for this session.";

        if (Commands.SplitChapterToggles.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleSplitChapters(settings);
            printer.Info(SummarizeToggle("Split Chapters", settings.SplitChapters));
            return Result.Ok(NextAction.Continue);
        }

        if (Commands.EmbedImagesToggles.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleEmbedImages(settings);
            printer.Info(SummarizeToggle("Embed Images", settings.EmbedImages));
            return Result.Ok(NextAction.Continue);
        }

        if (Commands.QuietModeToggles.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleQuietMode(settings);
            printer.Info(SummarizeToggle("Quiet Mode", settings.QuietMode));
            printer.ShowDebug(!settings.QuietMode);
            return Result.Ok(NextAction.Continue);
        }

        if (command.StartsWith(Commands.UpdateAudioFormatPrefix, StringComparison.InvariantCultureIgnoreCase))
        {
            var format = command.Replace(Commands.UpdateAudioFormatPrefix, string.Empty).ToLowerInvariant();

            if (format == string.Empty)
            {
                return Result.Fail($"You must append a supported audio format (e.g., \"best\" or \"m4a\").");
            }

            var updateResult = SettingsAdapter.UpdateAudioFormat(settings, format);
            if (updateResult.IsError)
            {
                return Result.Fail(updateResult.ErrorValue);
            }

            settings = updateResult.ResultValue;
            printer.Info(SummarizeUpdate("Audio Format", settings.AudioFormat));
            return Result.Ok(NextAction.Continue);
        }

        if (command.StartsWith(Commands.UpdateAudioQualityPrefix, StringComparison.InvariantCultureIgnoreCase))
        {
            var inputQuality = command.Replace(Commands.UpdateAudioQualityPrefix, string.Empty);

            if (inputQuality == string.Empty)
            {
                return Result.Fail($"You must enter a number representing an audio quality.");
            }

            if (!byte.TryParse(inputQuality, out var quality))
            {
                return Result.Fail($"\"{inputQuality}\" is an invalid quality value.");
            }

            var updateResult = SettingsAdapter.UpdateAudioQuality(settings, quality);
            if (updateResult.IsError)
            {
                return Result.Fail(updateResult.ErrorValue); // For out-of-range values
            }

            settings = updateResult.ResultValue;
            printer.Info(SummarizeUpdate("Audio Quality", settings.AudioQuality.ToString()));
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
