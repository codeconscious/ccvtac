namespace CCVTAC.Console

open System
open System.Threading
open CCVTAC.Console.Downloading
open CCVTAC.Console.Downloading.Downloading
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console.Settings.TagFormat
open CCVTAC.Console.Settings.Settings.Validation
open CCVTAC.Console.Settings.Settings.IO
open CCVTAC.Console.Settings.Settings.LiveUpdating
open Spectre.Console
open CCVTAC.Console.InputHelper
open Utilities
open Startwatch.Library

module Orchestrator =
    type NextAction =
        | Continue = 0uy
        | QuitAtUserRequest = 1uy
        | QuitDueToErrors = 2uy

    let summarizeInput
        (categorizedInputs: CategorizedInput list)
        (counts: CategoryCounts)
        (printer: Printer)
        : unit
        =

        if categorizedInputs.Length > 1 then
            let urlCount = counts[InputCategory.Url]
            let cmdCount = counts[InputCategory.Command]

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
                if hasText urlSummary && hasText commandSummary then " and " else String.Empty

            printer.Info $"Batch of %s{urlSummary}%s{connector}%s{commandSummary} entered."

            for input in categorizedInputs do
                printer.Info(sprintf " • %s" input.Text)

            Printer.EmptyLines 1uy

    let sleep (sleepSeconds: uint16) : unit =
        let mutable remainingSeconds = sleepSeconds

        AnsiConsole
            .Status()
            .Start($"Sleeping for %d{sleepSeconds} seconds...",
                   fun ctx ->
                        ctx.Spinner(Spinner.Known.Star) |> ignore
                        ctx.SpinnerStyle(Style.Parse("blue")) |> ignore

                        while remainingSeconds > 0us do
                            ctx.Status $"Sleeping for %d{remainingSeconds} seconds..." |> ignore
                            remainingSeconds <- remainingSeconds - 1us
                            Thread.Sleep 1000
                   )

    let processUrl
        (url: string)
        (settings: UserSettings)
        (resultTracker: ResultTracker<string>)
        (history: History)
        (urlInputTime: DateTime)
        (batchSize: int)
        (urlIndex: int)
        (printer: Printer)
        : Result<NextAction, string> =

        match Directories.warnIfAnyFiles settings.WorkingDirectory 10 with
        | Error firstErr ->
            printer.FirstError firstErr
            Ok NextAction.QuitDueToErrors
        | Ok () ->
            if urlIndex > 1 then // Don't sleep for the first URL.
                sleep settings.SleepSecondsBetweenURLs
                printer.Info($"Slept for %d{settings.SleepSecondsBetweenURLs} second(s).", appendLines = 1uy)

            if batchSize > 1 then
                printer.Info(sprintf "Processing group %d of %d..." urlIndex batchSize)

            let jobWatch = Watch()

            match Downloading.mediaTypeWithIds url with
            | Error e ->
                let errorMsg = $"URL parse error: %s{e}"
                printer.Error errorMsg
                Error errorMsg
            | Ok mediaType ->
                printer.Info(sprintf "%s URL '%s' detected." (mediaType.GetType().Name) url)
                history.Append(url, urlInputTime, printer)

                let downloadResult = Downloader.run mediaType settings printer
                resultTracker.RegisterResult(url, downloadResult)

                match downloadResult with
                | Error e ->
                    let errorMsg = $"Download error: %s{e}"
                    printer.Error errorMsg
                    Error errorMsg
                | Ok s ->
                    printer.Debug $"Successfully downloaded \"%s{s}\" format."
                    PostProcessor.run settings mediaType printer

                    let groupClause =
                        if batchSize > 1
                        then $" (group %d{urlIndex} of %d{batchSize})"
                        else String.Empty

                    printer.Info $"Processed '%s{url}'%s{groupClause} in %s{jobWatch.ElapsedFriendly}."
                    Ok NextAction.Continue

    let equalsIgnoreCase (a: string) (b: string) =
        String.Equals(a, b, StringComparison.InvariantCultureIgnoreCase)

    let seqContainsIgnoreCase (seq: seq<string>) (value: string) =
        seq |> Seq.exists (fun s -> equalsIgnoreCase s value)

    let startsWithIgnoreCase (text: string) (prefix: string) =
        text.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)

    let summarizeToggle settingName setting =
        sprintf "%s was toggled to %s for this session." settingName (if setting then "ON" else "OFF")

    let summarizeUpdate settingName setting =
        sprintf "%s was updated to \"%s\" for this session." settingName setting

    let processCommand
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
            history.ShowRecent printer
            Ok NextAction.Continue

        // Update downloader
        elif seqContainsIgnoreCase Commands.UpdateDownloader command then
            Updater.run settings printer |> ignore
            Ok NextAction.Continue

        // Settings summary
        elif seqContainsIgnoreCase Commands.SettingsSummary command then
            Settings.printSummary settings printer None
            Ok NextAction.Continue

        // Toggle split chapters
        elif seqContainsIgnoreCase Commands.SplitChapterToggles command then
            settings <- toggleSplitChapters(settings)
            printer.Info(summarizeToggle "Split Chapters" settings.SplitChapters)
            Ok NextAction.Continue

        // Toggle embed images
        elif seqContainsIgnoreCase Commands.EmbedImagesToggles command then
            settings <- toggleEmbedImages(settings)
            printer.Info(summarizeToggle "Embed Images" settings.EmbedImages)
            Ok NextAction.Continue

        // Toggle quiet mode
        elif seqContainsIgnoreCase Commands.QuietModeToggles command then
            settings <- toggleQuietMode(settings)
            printer.Info(summarizeToggle "Quiet Mode" settings.QuietMode)
            printer.ShowDebug(not settings.QuietMode)
            Ok NextAction.Continue

        // Update audio formats prefix
        elif startsWithIgnoreCase command Commands.UpdateAudioFormatPrefix then
            let format = command.Replace(Commands.UpdateAudioFormatPrefix, "").ToLowerInvariant()
            if String.IsNullOrEmpty format then
                Error "You must append one or more supported audio format separated by commas (e.g., \"m4a,opus,best\")."
            else
                let updateResult = updateAudioFormat settings format
                match updateResult with
                | Error e -> Error e
                | Ok x ->
                    settings <- x
                    printer.Info(summarizeUpdate "Audio Formats" (String.Join(", ", settings.AudioFormats)))
                    Ok NextAction.Continue
                // if updateResult.IsError then Error updateResult.ErrorValue
                // else
                //     settings <- updateResult.ResultValue
                //     printer.Info(summarizeUpdate "Audio Formats" (String.Join(", ", settings.AudioFormats)))
                //     Ok NextAction.Continue

        // Update audio quality prefix
        elif startsWithIgnoreCase command Commands.UpdateAudioQualityPrefix then
            let inputQuality = command.Replace(Commands.UpdateAudioQualityPrefix, "")
            if String.IsNullOrEmpty inputQuality then
                Error "You must enter a number representing an audio quality."
            else
                match Byte.TryParse inputQuality with
                | true, quality ->
                    let updateResult = updateAudioQuality settings quality
                    match updateResult with
                    | Error e -> Error e
                    | Ok x ->
                        settings <- x
                        printer.Info(summarizeUpdate "Audio Quality" (settings.AudioQuality.ToString()))
                        Ok NextAction.Continue
                    // if updateResult.IsError then Error updateResult.ErrorValue
                    // else
                    //     settings <- updateResult.ResultValue
                    //     printer.Info(summarizeUpdate "Audio Quality" (settings.AudioQuality.ToString()))
                    //     Ok NextAction.Continue
                | _ ->
                    Error (sprintf "\"%s\" is an invalid quality value." inputQuality)

        // Unknown command
        else
            Error (sprintf "\"%s\" is not a valid command. Enter \"%scommands\" to see a list of commands." command (string Commands.Prefix))


    /// Processes a single user request, from input to downloading and file post-processing.
    /// Returns the next action the application should take (e.g., continue or quit).
    let processBatch
        (categorizedInputs: CategorizedInput list)
        (categoryCounts: CategoryCounts)
        (settings: byref<UserSettings>)
        (resultTracker: ResultTracker<string>)
        (history: History)
        (printer: Printer)
        : NextAction =
        let inputTime = DateTime.Now
        let mutable nextAction = NextAction.Continue
        let watch = Watch()
        let batchResults = ResultTracker<NextAction>(printer)
        let mutable inputIndex = 0

        for input in categorizedInputs do
            let mutable stop = false
            // increment input index before passing to ProcessUrl to mirror ++inputIndex
            inputIndex <- inputIndex + 1

            let result =
                match input.Category with
                | InputCategory.Command ->
                    processCommand input.Text &settings history printer
                | InputCategory.Url ->
                    processUrl input.Text settings resultTracker history inputTime categoryCounts[InputCategory.Url] inputIndex printer

            batchResults.RegisterResult(input.Text, result)

            match result with
            | Error e -> printer.Error e
            | Ok action ->
                nextAction <- action
                if nextAction <> NextAction.Continue then
                    stop <- true

        if categoryCounts[InputCategory.Url] > 1 then
            printer.Info(sprintf "%sFinished with batch of %d URLs in %s."
                            Environment.NewLine
                            categoryCounts[InputCategory.Url]
                            watch.ElapsedFriendly)
            batchResults.PrintBatchFailures()

        nextAction

    /// Ensures the download environment is ready, then initiates the UI input and download process.
    let start (settings: UserSettings) (printer: Printer) : unit =
        // The working directory should start empty. Give the user a chance to empty it.
        match Directories.warnIfAnyFiles settings.WorkingDirectory 10 with
        | Error firstErr ->
            printer.FirstError firstErr

            match Directories.askToDeleteAllFiles settings.WorkingDirectory printer with
            | Ok deletedCount ->
                printer.Info $"%d{deletedCount} file(s) deleted."
            | Error err ->
                printer.FirstError err
                printer.Info "Aborting..."
                ()
        | Ok () ->
            let results = ResultTracker<string>(printer)
            let history = History(settings.HistoryFile, settings.HistoryDisplayCount)
            let mutable nextAction = NextAction.Continue
            let mutable settingsRef = settings

            while nextAction = NextAction.Continue do
                let input = printer.GetInput InputHelper.Prompt
                let splitInputs = InputHelper.SplitInput input

                if splitInputs.IsEmpty then
                    printer.Error (sprintf "Invalid input. Enter only URLs or commands beginning with \"%c\"." Commands.Prefix)
                else
                    let categorizedInputs = InputHelper.CategorizeInputs splitInputs
                    let categoryCounts = InputHelper.CountCategories categorizedInputs

                    summarizeInput categorizedInputs categoryCounts printer

                    // ProcessBatch may modify settings; reflect that by using a mutable reference
                    nextAction <- processBatch categorizedInputs categoryCounts &settingsRef results history printer

            results.PrintSessionSummary()

