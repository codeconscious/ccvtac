using CCVTAC.Console.Settings;
using CCVTAC.Console.Downloading;
using System.Threading;
using Spectre.Console;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

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
            printer.Print("Pass '--help' to this program for more information.");
            return;
        }

        // The working directory should start empty.
        var emptyDirResult = IoUtilties.Directories.WarnIfDirectoryHasFiles(settings.WorkingDirectory, printer);
        if (emptyDirResult.IsFailed)
        {
            printer.Print($"Aborting...");
            return;
        }

        var resultTracker = new ResultTracker(printer);
        var history = new History(settings.HistoryFile, settings.HistoryDisplayCount);

        while (true)
        {
            var nextAction = ProcessBatch(ref settings, resultTracker, history, printer);

            if (nextAction is not NextAction.Continue)
            {
                break;
            }
        }

        resultTracker.PrintFinalSummary();
    }

    internal record CategorizedInput(string Text, InputType InputType);

    /// <summary>
    /// Processes a single user request, from input to downloading and file post-processing.
    /// </summary>
    /// <returns>Returns the next action the application should take (e.g., continue or quit).</returns>
    private static NextAction ProcessBatch(
        ref UserSettings settings,
        ResultTracker resultTracker,
        History history,
        Printer printer)
    {
        string userInput = printer.GetInput(Commands.InputPrompt);

        var inputTime = DateTime.Now;
        Watch watch = new();

        var splitInputs = InputHelper.SplitInputs(userInput);

        if (splitInputs.IsEmpty)
        {
            printer.Error($"Invalid input. Enter only URLs or commands beginning with \"{Commands.Prefix}\".");
            return NextAction.Continue;
        }

        var categorizedInputs = splitInputs
            .Select(input =>
                new CategorizedInput(
                    input,
                    input.StartsWith(Commands.Prefix)
                        ? InputType.Command
                        : InputType.Url)
            )
            .ToImmutableList();

        int urlCount = categorizedInputs.Count(i => i.InputType == InputType.Url);
        SummarizeInput(categorizedInputs, urlCount, printer);

        nuint currentBatch = 0;
        bool haveProcessedAny = false;

        foreach (CategorizedInput input in categorizedInputs)
        {
            if (input.InputType is InputType.Command)
            {
                var result = ProcessCommand(input.Text, ref settings, history, printer);

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

                continue;
            }

            string url = input.Text;

            var tempFiles = IoUtilties.Directories.GetDirectoryFileNames(settings.WorkingDirectory);
            if (tempFiles.Any())
            {
                printer.Error($"{tempFiles.Count} file(s) unexpectedly found in the working directory ({settings.WorkingDirectory}), so will abort:");
                tempFiles.ForEach(file => printer.Warning($"• {file}"));
                return NextAction.QuitDueToErrors;
            }

            if (haveProcessedAny) // Don't sleep for the very first URL.
            {
                Sleep(settings.SleepSecondsBetweenBatches);
                printer.Print($"Slept for {settings.SleepSecondsBetweenBatches} second(s).", appendLines: 1);
            }
            else
            {
                haveProcessedAny = true;
            }

            if (urlCount > 1)
            {
                printer.Print($"Processing batch {++currentBatch} of {urlCount}...");
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
        }

        if (urlCount > 1)
        {
            printer.Print($"\nAll done with {urlCount} batches in {watch.ElapsedFriendly}.");
        }

        return NextAction.Continue;
    }

    private static Result<NextAction> ProcessCommand(
        string command,
        ref UserSettings settings,
        History history,
        Printer printer)
    {
        if (Commands._quitCommands.CaseInsensitiveContains(command))
        {
            return Result.Ok(NextAction.QuitAtUserRequest);
        }

        if (Commands._historyCommands.CaseInsensitiveContains(command))
        {
            history.ShowRecent(printer);
            return Result.Ok(NextAction.Continue);
        }

        if (Commands._showSettingsCommands.CaseInsensitiveContains(command))
        {
            SettingsAdapter.PrintSummary(settings, printer);
            return Result.Ok(NextAction.Continue);
        }

        if (Commands._toggleSplitChapterCommands.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleSplitChapters(settings);
            SettingsAdapter.PrintSummary(
                settings, printer, "Split Chapters was toggled for this session.");
            return Result.Ok(NextAction.Continue);
        }

        if (Commands._toggleEmbedImagesCommands.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleEmbedImages(settings);
            SettingsAdapter.PrintSummary(
                settings, printer, "Embed Images was toggled for this session.");
            return Result.Ok(NextAction.Continue);
        }

        if (Commands._toggleVerboseOutputCommands.CaseInsensitiveContains(command))
        {
            settings = SettingsAdapter.ToggleVerboseOutput(settings);
            SettingsAdapter.PrintSummary(
                settings, printer, "Verbose Output was toggled for this session.");
            return Result.Ok(NextAction.Continue);
        }

        return Result.Fail($"\"{command}\" is not a valid command.");
    }

    private static void SummarizeInput(
        ImmutableList<CategorizedInput> categorizedInputs,
        int urlCount,
        Printer printer)
    {
        var commandCount = categorizedInputs.Count - urlCount;

        if (categorizedInputs.Count > 1)
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
                if (input.InputType is InputType.Url)
                    printer.Print($"      URL: {input.Text}");
                else
                    printer.Print($"  Command: {input.Text}");
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

    internal enum InputType { Url, Command }
}
