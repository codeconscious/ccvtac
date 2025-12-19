namespace CCVTAC.Console

open CCVTAC.Console.Downloading
open CCVTAC.Console.InputHelper
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console.Settings.Settings.LiveUpdating
open Startwatch.Library
open System

module Orchestrator =

    type NextAction =
        | Continue
        | QuitAtUserRequest
        | QuitDueToErrors

    type BatchResults =
        { NextAction: NextAction
          UpdatedSettings: UserSettings option }

    let summarizeInput
        (categorizedInputs: CategorizedInput list)
        (counts: CategoryCounts)
        (printer: Printer)
        : unit =

        if List.hasMultiple categorizedInputs then
            let urlSummary = String.pluralizeWithCount "URL" "URLs" counts[InputCategory.Url]
            let cmdSummary = String.pluralizeWithCount "command" "commands" counts[InputCategory.Command]

            printer.Info <|
                match counts[InputCategory.Url], counts[InputCategory.Command] with
                | u, c when u > 0 && c > 0 -> $"Batch of %s{urlSummary} and %s{cmdSummary} entered:"
                | u, _ when u > 0 ->          $"Batch of %s{urlSummary} entered:"
                | _, c when c > 0 ->          $"Batch of %s{cmdSummary} entered:"
                | _, _ ->                      "No URLs or commands were entered!"

            for input in categorizedInputs do
                printer.Info $" • %s{input.Text}"

            Printer.EmptyLines 1uy

    let processUrl
        (url: string)
        (settings: UserSettings)
        (resultTracker: ResultTracker<string>)
        (history: History)
        (urlInputTime: DateTime)
        (batchSize: int)
        (urlIndex: int)
        (printer: Printer)
        : Result<BatchResults, string> =

        match Directories.warnIfAnyFiles 10 settings.WorkingDirectory with
        | Error err ->
            printer.Error err
            Ok { NextAction = NextAction.QuitDueToErrors; UpdatedSettings = None }
        | Ok () ->
            if urlIndex > 1 then // Don't sleep for the first URL.
                settings.SleepSecondsBetweenURLs
                |> String.pluralize "second" "seconds"
                |> fun secondsLabel ->
                    sleep
                        (fun seconds -> $"Sleeping for {seconds} {secondsLabel}...")
                        (fun seconds -> $"Slept for {seconds} {secondsLabel}.")
                        settings.SleepSecondsBetweenURLs
                |> fun msg -> printer.Info($"{String.newLine}{msg}", appendLines = 1uy)

            if batchSize > 1 then
                printer.Info $"Processing item %d{urlIndex} of %d{batchSize}..."

            let jobWatch = Watch()

            match Downloading.mediaTypeWithIds url with
            | Error e ->
                let errorMsg = $"URL parse error: %s{e}"
                printer.Error errorMsg
                Error errorMsg
            | Ok mediaType ->
                printer.Info $"%s{mediaType.GetType().Name} URL '%s{url}' detected."
                history.Append(url, urlInputTime, printer)

                let downloadResult = Downloader.run mediaType settings printer
                resultTracker.RegisterResult(url, downloadResult)

                match downloadResult with
                | Error errs ->
                    errs
                    |> List.map (sprintf "Media download error: %s")
                    |> String.concat String.newLine
                    |> Error
                | Ok msgs ->
                    printer.Debug "Media download(s) successful!"
                    msgs |> List.iter printer.Info
                    PostProcessor.run settings mediaType printer

                    let groupClause =
                        if batchSize > 1
                        then $" (item %d{urlIndex} of %d{batchSize})"
                        else String.Empty

                    printer.Info $"Processed '%s{url}'%s{groupClause} in %s{jobWatch.ElapsedFriendly}."

                    Ok { NextAction = NextAction.Continue
                         UpdatedSettings = None }

    let summarizeToggle settingName setting =
        sprintf "%s was toggled to %s for this session." settingName (if setting then "ON" else "OFF")

    let summarizeUpdate settingName setting =
        sprintf "%s was updated to \"%s\" for this session." settingName setting

    let processCommand
        (command: string)
        (settings: UserSettings)
        (history: History)
        (printer: Printer)
        : Result<BatchResults, string> =

        let checkCommand = List.caseInsensitiveContains command

        // Help
        if String.equalIgnoringCase Commands.helpCommand command then
            for kvp in Commands.summary do
                printer.Info(kvp.Key)
                printer.Info $"    %s{kvp.Value}"
            Ok { NextAction = NextAction.Continue; UpdatedSettings = None }

        // Quit
        elif checkCommand Commands.quitCommands then
            Ok { NextAction = NextAction.QuitAtUserRequest; UpdatedSettings = None }

        // History
        elif checkCommand Commands.history then
            history.ShowRecent printer
            Ok { NextAction = NextAction.Continue; UpdatedSettings = None }

        // Update downloader
        elif checkCommand Commands.updateDownloader then
            Updater.run settings printer |> ignore
            Ok { NextAction = NextAction.Continue; UpdatedSettings = None }

        // Settings summary
        elif checkCommand Commands.settingsSummary then
            Settings.printSummary settings printer None
            Ok { NextAction = NextAction.Continue; UpdatedSettings = None }

        // Toggle split chapters
        elif checkCommand Commands.splitChapterToggles then
            let newSettings = toggleSplitChapters settings
            printer.Info(summarizeToggle "Split Chapters" newSettings.SplitChapters)
            Ok { NextAction = NextAction.Continue; UpdatedSettings = Some newSettings }

        // Toggle embed images
        elif checkCommand Commands.embedImagesToggles then
            let newSettings = toggleEmbedImages settings
            printer.Info(summarizeToggle "Embed Images" newSettings.EmbedImages)
            Ok { NextAction = NextAction.Continue; UpdatedSettings = Some newSettings }

        // Toggle quiet mode
        elif checkCommand Commands.quietModeToggles then
            let newSettings = toggleQuietMode settings
            printer.Info(summarizeToggle "Quiet Mode" newSettings.QuietMode)
            printer.ShowDebug(not newSettings.QuietMode)
            Ok { NextAction = NextAction.Continue; UpdatedSettings = Some newSettings }

        // Update audio formats
        elif command |> String.startsWithIgnoreCase Commands.updateAudioFormatPrefix then
            let format = command.Replace(Commands.updateAudioFormatPrefix, String.Empty).ToLowerInvariant()
            if String.hasNoText format then
                Error "You must append one or more supported audio formats separated by commas (e.g., \"m4a,opus,best\")."
            else
                let updateResult = updateAudioFormat settings format
                match updateResult with
                | Error err -> Error err
                | Ok newSettings ->
                    printer.Info(summarizeUpdate "Audio Formats" (String.Join(", ", newSettings.AudioFormats)))
                    Ok { NextAction = NextAction.Continue; UpdatedSettings = Some newSettings }

        // Update audio quality
        elif command |> String.startsWithIgnoreCase Commands.updateAudioQualityPrefix then
            let inputQuality = command.Replace(Commands.updateAudioQualityPrefix, String.Empty)
            if String.hasNoText inputQuality then
                Error "You must enter a number representing an audio quality between 10 (lowest) and 0 (highest)."
            else
                match Byte.TryParse inputQuality with
                | true, quality ->
                    let updateResult = updateAudioQuality settings quality
                    match updateResult with
                    | Error err ->
                        Error err
                    | Ok updatedSettings ->
                        printer.Info(summarizeUpdate "Audio Quality" (updatedSettings.AudioQuality.ToString()))
                        Ok { NextAction = NextAction.Continue; UpdatedSettings = Some updatedSettings }
                | _ ->
                    Error $"\"%s{inputQuality}\" is an invalid quality value."

        // Unknown command
        else
            Error <|
                sprintf "\"%s\" is not a valid command. Enter \"%shelp\" to see a list of commands."
                    command
                    (string Commands.prefix)


    /// Processes a single user request, from input to downloading and file post-processing.
    /// Returns the next action the application should take (e.g., continue or quit).
    let processBatch
        (categorizedInputs: CategorizedInput list)
        (categoryCounts: CategoryCounts)
        (settings: UserSettings)
        (resultTracker: ResultTracker<string>)
        (history: History)
        (printer: Printer)
        : BatchResults =

        let inputTime = DateTime.Now
        let watch = Watch()
        let batchResults = ResultTracker<BatchResults> printer
        let mutable nextAction = NextAction.Continue
        let mutable inputIndex = 0
        let mutable currentSettings = settings

        for input in categorizedInputs do
            let mutable stop = false
            inputIndex <- inputIndex + 1

            let result =
                match input.Category with
                | InputCategory.Command ->
                    processCommand input.Text currentSettings history printer
                | InputCategory.Url ->
                    processUrl input.Text currentSettings resultTracker history inputTime
                               categoryCounts[InputCategory.Url] inputIndex printer

            batchResults.RegisterResult(input.Text, result)

            match result with
            | Error e ->
                printer.Error e
            | Ok r ->
                nextAction <- r.NextAction
                match r.UpdatedSettings with
                    | None -> ()
                    | Some us -> currentSettings <- us
                if nextAction <> NextAction.Continue then
                    stop <- true

        if categoryCounts[InputCategory.Url] > 1 then
            printer.Info(sprintf "%sFinished with batch of %d URLs in %s."
                            String.newLine
                            categoryCounts[InputCategory.Url]
                            watch.ElapsedFriendly)
            batchResults.PrintBatchFailures()

        { NextAction = nextAction
          UpdatedSettings = Some currentSettings }

    /// Ensures the download environment is ready, then initiates the input and download process.
    let start (settings: UserSettings) (printer: Printer) : unit =
        // The working directory should start empty. Give the user a chance to empty it.
        match Directories.warnIfAnyFiles 10 settings.WorkingDirectory with
        | Ok () -> ()
        | Error filesFoundErr ->
            printer.Error filesFoundErr
            Directories.askToDeleteAllFiles settings.WorkingDirectory printer |> function
            | Ok results -> Directories.printDeletionResults printer results
            | Error deletionError ->
                printer.Error deletionError
                printer.Info "Aborting..."

        let results = ResultTracker<string> printer
        let history = History(settings.HistoryFile, settings.HistoryDisplayCount)
        let mutable nextAction = NextAction.Continue
        let mutable currentSettings = settings

        while nextAction = NextAction.Continue do
            let input = printer.GetInput prompt
            let splitInputs = splitInputText input

            if List.isEmpty splitInputs then
                printer.Error $"Invalid input. Enter only URLs or commands beginning with \"%c{Commands.prefix}\"."
            else
                let categorizedInputs = categorizeInputs splitInputs
                let categoryCounts = countCategories categorizedInputs
                summarizeInput categorizedInputs categoryCounts printer
                let batchResult = processBatch categorizedInputs categoryCounts currentSettings results history printer
                nextAction <- batchResult.NextAction
                match batchResult.UpdatedSettings with
                | Some s -> currentSettings <- s
                | None -> ()

        results.PrintSessionSummary()

