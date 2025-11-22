namespace CCVTAC.Console

open System
open CCVTAC.Console.Downloading
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.Settings
open Spectre.Console
open CCVTAC.Console.InputHelper
open CCVTAC.FSharp.Settings

type Orchestrator() =

    /// Ensures the download environment is ready, then initiates the UI input and download process.
    static member Start (settings: UserSettings) (printer: Printer) : unit =
        // The working directory should start empty. Give the user a chance to empty it.
        match Directories.WarnIfAnyFiles(settings.WorkingDirectory, 10) with
        | Error firstErr ->
            printer.FirstError(firstErr)

            match Directories.AskToDeleteAllFiles(settings.WorkingDirectory, printer) with
            | Ok deletedCount ->
                printer.Info (sprintf "%d file(s) deleted." deletedCount)
            | Error err ->
                printer.FirstError(err)
                printer.Info "Aborting..."
                // abort Start by returning
                ()
        | Ok () ->
            // proceed

            let results = ResultTracker<string?>(printer)
            let history = History(settings.HistoryFile, settings.HistoryDisplayCount)
            let mutable nextAction = NextAction.Continue
            let mutable settingsRef = settings

            while nextAction = NextAction.Continue do
                let input = printer.GetInput InputHelper.Prompt
                let splitInputs = InputHelper.SplitInput input

                if splitInputs.IsEmpty then
                    printer.Error (sprintf "Invalid input. Enter only URLs or commands beginning with \"%c\"." Commands.Prefix)
                else
                    let categorizedInputs = InputHelper.CategorizeInputs(splitInputs)
                    let categoryCounts = InputHelper.CountCategories(categorizedInputs)

                    SummarizeInput(categorizedInputs, categoryCounts, printer)

                    // ProcessBatch may modify settings; reflect that by using a mutable reference
                    nextAction <- ProcessBatch(categorizedInputs, categoryCounts, &settingsRef, results, history, printer)

            results.PrintSessionSummary()

/// TODO: Redo?

    /// Processes a single user request, from input to downloading and file post-processing.
    /// Returns the next action the application should take (e.g., continue or quit).
    let private ProcessBatch
        (categorizedInputs: ImmutableArray<CategorizedInput>)
        (categoryCounts: CategoryCounts)
        (settings: byref<UserSettings>)
        (resultTracker: ResultTracker<string option>)
        (history: History)
        (printer: Printer)
        : NextAction =
        let inputTime = DateTime.Now
        let mutable nextAction = NextAction.Continue
        let watch = Watch()
        let batchResults = ResultTracker<NextAction>(printer)
        let mutable inputIndex = 0

        for input in categorizedInputs do
            // increment input index before passing to ProcessUrl to mirror ++inputIndex
            inputIndex <- inputIndex + 1

            let result =
                match input.Category with
                | InputCategory.Command ->
                    ProcessCommand(input.Text, &settings, history, printer)
                | InputCategory.Url ->
                    ProcessUrl(
                        input.Text,
                        settings,
                        resultTracker,
                        history,
                        inputTime,
                        categoryCounts.[InputCategory.Url],
                        inputIndex,
                        printer
                    )

            batchResults.RegisterResult(input.Text, result)

            if result.IsFailed then
                printer.Error(result.Errors.First().Message)
            else
                nextAction <- result.Value
                if nextAction <> NextAction.Continue then
                    // break out early
                    break

        if categoryCounts.[InputCategory.Url] > 1 then
            printer.Info(sprintf "%sFinished with batch of %d URLs in %s."
                            Environment.NewLine
                            categoryCounts.[InputCategory.Url]
                            watch.ElapsedFriendly)
            batchResults.PrintBatchFailures()

        nextAction

open System

    let private ProcessUrl
        (url: string)
        (settings: UserSettings)
        (resultTracker: ResultTracker<string option>)
        (history: History)
        (urlInputTime: DateTime)
        (batchSize: int)
        (urlIndex: int)
        (printer: Printer)
        : Result<NextAction, string> =

        match Directories.WarnIfAnyFiles(settings.WorkingDirectory, 10) with
        | Error firstErr ->
            printer.FirstError(firstErr)
            Ok NextAction.QuitDueToErrors
        | Ok () ->
            // Don't sleep for the very first URL.
            if urlIndex > 1 then
                Threading.Thread.Sleep(settings.SleepSecondsBetweenURLs * 1000)
                printer.Info(sprintf "Slept for %d second(s)." settings.SleepSecondsBetweenURLs, appendLines = 1)

            if batchSize > 1 then
                printer.Info(sprintf "Processing group %d of %d..." urlIndex batchSize)

            let jobWatch = Watch()

            match Downloader.WrapUrlInMediaType(url) with
            | Error e ->
                let errorMsg = sprintf "URL parse error: %s" (e |> Seq.map (fun er -> er.Message) |> Seq.head)
                printer.Error(errorMsg)
                Error errorMsg
            | Ok mediaType ->
                printer.Info(sprintf "%s URL '%s' detected." (mediaType.GetType().Name) url)
                history.Append(url, urlInputTime, printer)

                let downloadResult = Downloader.Run(mediaType, settings, printer)
                resultTracker.RegisterResult(url, downloadResult)

                if downloadResult.IsFailed then
                    let errorMsg = sprintf "Download error: %s" (downloadResult.Errors |> Seq.map (fun er -> er.Message) |> Seq.head)
                    printer.Error(errorMsg)
                    Error errorMsg
                else
                    printer.Debug(sprintf "Successfully downloaded \"%s\" format." downloadResult.Value)
                    PostProcessor.Run(settings, mediaType, printer)

                    let groupClause = if batchSize > 1 then sprintf " (group %d of %d)" urlIndex batchSize else String.Empty
                    printer.Info(sprintf "Processed '%s'%s in %s." url groupClause jobWatch.ElapsedFriendly)
                    Ok NextAction.Continue

    let private ProcessCommand

        let private equalsIgnoreCase (a: string) (b: string) =
            String.Equals(a, b, StringComparison.InvariantCultureIgnoreCase)

        let private seqContainsIgnoreCase (seq: seq<string>) (value: string) =
            seq |> Seq.exists (fun s -> equalsIgnoreCase s value)

        let private startsWithIgnoreCase (text: string) (prefix: string) =
            text.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)

        let private summarizeToggle settingName setting =
            sprintf "%s was toggled to %s for this session." settingName (if setting then "ON" else "OFF")

        let private summarizeUpdate settingName setting =
            sprintf "%s was updated to \"%s\" for this session." settingName setting

        let private ProcessCommand
            (command: string)
            (settings: byref<UserSettings>)
            (history: History)
            (printer: Printer)
            : Result<NextAction, string> =

            // Help
            if equalsIgnoreCase Commands.HelpCommand command then
                for kvp in Commands.Summary do
                    printer.Info(kvp.Key)
                    printer.Info(sprintf "    %s" kvp.Value)
                Ok NextAction.Continue

            // Quit
            elif seqContainsIgnoreCase Commands.QuitCommands command then
                Ok NextAction.QuitAtUserRequest

            // History
            elif seqContainsIgnoreCase Commands.History command then
                history.ShowRecent(printer)
                Ok NextAction.Continue

            // Update downloader
            elif seqContainsIgnoreCase Commands.UpdateDownloader command then
                Updater.Run(settings, printer) |> ignore
                Ok NextAction.Continue

            // Settings summary
            elif seqContainsIgnoreCase Commands.SettingsSummary command then
                SettingsAdapter.PrintSummary(settings, printer)
                Ok NextAction.Continue

            // Toggle split chapters
            elif seqContainsIgnoreCase Commands.SplitChapterToggles command then
                settings <- SettingsAdapter.ToggleSplitChapters(settings)
                printer.Info(summarizeToggle "Split Chapters" settings.SplitChapters)
                Ok NextAction.Continue

            // Toggle embed images
            elif seqContainsIgnoreCase Commands.EmbedImagesToggles command then
                settings <- SettingsAdapter.ToggleEmbedImages(settings)
                printer.Info(summarizeToggle "Embed Images" settings.EmbedImages)
                Ok NextAction.Continue

            // Toggle quiet mode
            elif seqContainsIgnoreCase Commands.QuietModeToggles command then
                settings <- SettingsAdapter.ToggleQuietMode(settings)
                printer.Info(summarizeToggle "Quiet Mode" settings.QuietMode)
                printer.ShowDebug(not settings.QuietMode)
                Ok NextAction.Continue

            // Update audio formats prefix
            elif startsWithIgnoreCase command Commands.UpdateAudioFormatPrefix then
                let format = command.Replace(Commands.UpdateAudioFormatPrefix, "").ToLowerInvariant()
                if String.IsNullOrEmpty format then
                    Error "You must append one or more supported audio format separated by commas (e.g., \"m4a,opus,best\")."
                else
                    let updateResult = SettingsAdapter.UpdateAudioFormat(settings, format)
                    if updateResult.IsError then Error updateResult.ErrorValue
                    else
                        settings <- updateResult.ResultValue
                        printer.Info(summarizeUpdate "Audio Formats" (String.Join(", ", settings.AudioFormats)))
                        Ok NextAction.Continue

            // Update audio quality prefix
            elif startsWithIgnoreCase command Commands.UpdateAudioQualityPrefix then
                let inputQuality = command.Replace(Commands.UpdateAudioQualityPrefix, "")
                if String.IsNullOrEmpty inputQuality then
                    Error "You must enter a number representing an audio quality."
                else
                    match Byte.TryParse(inputQuality) with
                    | (true, quality) ->
                        let updateResult = SettingsAdapter.UpdateAudioQuality(settings, quality)
                        if updateResult.IsError then Error updateResult.ErrorValue
                        else
                            settings <- updateResult.ResultValue
                            printer.Info(summarizeUpdate "Audio Quality" (settings.AudioQuality.ToString()))
                            Ok NextAction.Continue
                    | _ ->
                        Error (sprintf "\"%s\" is an invalid quality value." inputQuality)

            // Unknown command
            else
                Error (sprintf "\"%s\" is not a valid command. Enter \"%scommands\" to see a list of commands." command (string Commands.Prefix))



    let summarizeInput

        type NextAction =
            | Continue = 0uy
            | QuitAtUserRequest = 1uy
            | QuitDueToErrors = 2uy

        let private SummarizeInput
            (categorizedInputs: ImmutableArray<CategorizedInput>)
            (counts: CategoryCounts)
            (printer: Printer)
            : unit =
            if categorizedInputs.Length > 1 then
                let urlCount = counts.[InputCategory.Url]
                let cmdCount = counts.[InputCategory.Command]

                let urlSummary =
                    match urlCount with
                    | 1 -> "1 URL"
                    | n when n > 1 -> sprintf "%d URLs" n
                    | _ -> String.Empty

                let commandSummary =
                    match cmdCount with
                    | 1 -> "1 command"
                    | n when n > 1 -> sprintf "%d commands" n
                    | _ -> String.Empty

                let connector =
                    if urlSummary.HasText() && commandSummary.HasText() then " and " else String.Empty

                printer.Info(sprintf "Batch of %s%s%s entered." urlSummary connector commandSummary)

                for input in categorizedInputs do
                    printer.Info(sprintf " • %s" input.Text)

                Printer.EmptyLines(1)

        let private Sleep (sleepSeconds: uint16) : unit =
            // Use a mutable remainingSeconds to mirror the C# behavior
            let mutable remainingSeconds = sleepSeconds

            AnsiConsole
                .Status()
                .Start(sprintf "Sleeping for %d seconds..." sleepSeconds,
                       fun ctx ->
                            ctx.Spinner(Spinner.Known.Star)
                            ctx.SpinnerStyle(Style.Parse("blue"))

                            while remainingSeconds > 0us do
                                ctx.Status(sprintf "Sleeping for %d seconds..." remainingSeconds)
                                remainingSeconds <- remainingSeconds - 1us
                                Thread.Sleep(1000)
                       )
            |> ignore
