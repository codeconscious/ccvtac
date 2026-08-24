namespace CCVTAC.Main

open CCVTAC.Main.Downloading
open CCVTAC.Main.InputHelper
open CCVTAC.Main.IoUtilities
open CCVTAC.Main.PostProcessing
open CCVTAC.Main.Settings
open CCVTAC.Main.Settings.Settings
open CCVTAC.Main.Settings.Settings.LiveUpdating
open CCFSharpUtils.Collections
open CCFSharpUtils.Text
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
        (printer: Printer)
        (categorizedInputs: CategorizedInput list)
        (counts: CategoryCounts)
        : unit =

        if List.hasMultiple categorizedInputs then
            let urlSummary = String.pluralizeSWithCount "URL" counts[Url]
            let cmdSummary = String.pluralizeSWithCount "command" counts[Command]

            printer.Info <|
                match counts[Url], counts[Command] with
                | u, c when u > 0 && c > 0 -> $"Batch of %s{urlSummary} and %s{cmdSummary} entered:"
                | u, _ when u > 0 ->          $"Batch of %s{urlSummary} entered:"
                | _, c when c > 0 ->          $"Batch of %s{cmdSummary} entered:"
                | _, _ ->                      "No URLs or commands were entered!"

            for input in categorizedInputs do
                printer.Info $" • %s{input.Text}"

            printer.EmptyLine()

    let processUrl
        (printer: Printer)
        (url: string)
        (settings: UserSettings)
        (resultTracker: ResultTracker<string>)
        (history: History)
        (urlInputTime: DateTime)
        (batchSize: int)
        (urlIndex: int)
        : Result<BatchResults, string> =

        match Directories.warnIfAnyFiles 10 settings.WorkingDirectory with
        | Error err ->
            printer.Error err
            Ok { NextAction = QuitDueToErrors; UpdatedSettings = None }
        | Ok () ->
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

                let downloadResult = Downloader.run printer mediaType settings
                resultTracker.RegisterResult(url, downloadResult)

                match downloadResult with
                | Error errs ->
                    errs
                    |> List.map (sprintf "Media download error: %s")
                    |> String.concat String.nl
                    |> Error
                | Ok message ->
                    printer.Debug "Download successful."
                    if String.hasText message then printer.Info message
                    PostProcessor.run printer settings mediaType

                    let groupClause =
                        if batchSize > 1
                        then $" (item %d{urlIndex} of %d{batchSize})"
                        else String.Empty

                    printer.Info $"Processed '%s{url}'%s{groupClause} in %s{jobWatch.ElapsedFriendly}."
                    Ok { NextAction = Continue; UpdatedSettings = None }

    let summarizeToggle settingName setting =
        sprintf "%s was toggled to %s for this session." settingName (if setting then "ON" else "OFF")

    let summarizeUpdate settingName setting =
        sprintf "%s was updated to \"%s\" for this session." settingName setting

    let processCommand
        (printer: Printer)
        (command: string)
        (settings: UserSettings)
        (history: History)
        : Result<BatchResults, string> =

        let checkCommand = List.containsIgnoreCase command

        // Help
        if String.equalIgnoreCase Commands.helpCommand command then
            for kvp in Commands.summary do
                printer.Info kvp.Key
                printer.Info $"    %s{kvp.Value}"
            Ok { NextAction = Continue; UpdatedSettings = None }

        // Quit
        elif checkCommand Commands.quitCommands then
            Ok { NextAction = QuitAtUserRequest; UpdatedSettings = None }

        // History
        elif checkCommand Commands.history then
            history.ShowRecent printer
            Ok { NextAction = Continue; UpdatedSettings = None }

        // Update media downloader
        elif checkCommand Commands.updateDownloader then
            Updater.run printer settings
            Ok { NextAction = Continue; UpdatedSettings = None }

        // Settings summary
        elif checkCommand Commands.settingsSummary then
            Settings.toTable settings |> printer.PrintTable
            Ok { NextAction = Continue; UpdatedSettings = None }

        // Toggle split chapters
        elif checkCommand Commands.splitChapterToggles then
            let newSettings = toggleSplitChapters settings
            printer.Info(summarizeToggle "Split Chapters" newSettings.SplitChapters)
            Ok { NextAction = Continue; UpdatedSettings = Some newSettings }

        // Toggle embed images
        elif checkCommand Commands.embedImagesToggles then
            let newSettings = toggleEmbedImages settings
            printer.Info(summarizeToggle "Embed Images" newSettings.EmbedImages)
            Ok { NextAction = Continue; UpdatedSettings = Some newSettings }

        // Toggle quiet mode
        elif checkCommand Commands.quietModeToggles then
            let newSettings = toggleQuietMode settings
            printer.Info(summarizeToggle "Quiet Mode" newSettings.QuietMode)
            printer.ShowDebug(not newSettings.QuietMode)
            Ok { NextAction = Continue; UpdatedSettings = Some newSettings }

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
                    Ok { NextAction = Continue; UpdatedSettings = Some newSettings }

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
                        Ok { NextAction = Continue; UpdatedSettings = Some updatedSettings }
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
        (printer: Printer)
        (inputs: CategorizedInput list)
        (categoryCounts: CategoryCounts)
        (settings: UserSettings)
        (resultTracker: ResultTracker<string>)
        (history: History)
        : BatchResults =

        let inputTime = DateTime.Now
        let batchWatch = Watch()
        let batchResults = ResultTracker<BatchResults> printer

        let printSleep (category: InputCategory) =
            if category.IsUrl then
                let seconds = settings.SleepSecondsBetweenURLs
                let label = String.pluralizeS "second" seconds
                sleep
                    (fun s -> $"Sleeping for {s} {label}...")
                    (fun s -> $"Slept for {s} {label}.")
                    seconds
                |> fun msg -> printer.Info($"{String.nl}{msg}", appendLines = 1uy)

        let processInput category text index : Result<BatchResults,string> =
            match category with
            | Command -> processCommand printer text settings history
            | Url -> processUrl printer text settings resultTracker history inputTime inputs.Length index

        let deleteLeftoverFiles dirName : Result<string,string> =
            match Directories.warnIfAnyFiles 10 dirName with
            | Ok () -> Ok "No leftover files found."
            | Error filesFoundErr ->
                printer.Error filesFoundErr // Might not need this.
                Directories.deleteAllFiles dirName |> function
                | Ok results ->
                    Directories.printDeletionResults printer results
                    Ok "Files deleted successfully."
                | Error deletionError ->
                    Error $"Error deleting leftover files after download: {deletionError}"

        let rec loop inputs settings' nextAction' index : NextAction * UserSettings * int =
            match inputs with
            | [] ->
                (nextAction', settings', index)
            | input :: remainingInputs when nextAction' = Continue ->
                let processResult = processInput input.Category input.Text index
                batchResults.RegisterResult(input.Text, processResult)

                // Deleting the files here might make debugging issues a bit tougher.
                match deleteLeftoverFiles settings.WorkingDirectory with
                | Error errMsg ->
                    printer.Error errMsg
                    (QuitDueToErrors, settings', index)
                | Ok message ->
                    printer.Debug message
                    match processResult with
                    | Error err ->
                        printer.Error err
                        if List.isNotEmpty remainingInputs then printSleep input.Category
                        loop remainingInputs settings' nextAction' (index + 1)
                    | Ok processResult ->
                        if List.isNotEmpty remainingInputs then printSleep input.Category
                        let newSettings = processResult.UpdatedSettings |> Option.defaultValue settings'
                        let newNextAction = processResult.NextAction
                        loop remainingInputs newSettings newNextAction (index + 1)
            | _ ->
                (nextAction', settings', index)

        let (finalNextAction, finalSettings, processedCount) =
            loop inputs settings Continue 1

        if categoryCounts[Url] > 1 then
            printer.Info(
                sprintf "%sFinished with batch of %d URLs in %s."
                    String.nl
                    categoryCounts[Url]
                    batchWatch.ElapsedFriendly
                )
            batchResults.PrintBatchFailures()

        if processedCount <= inputs.Length then
            let unprocessedInputs =
                inputs[processedCount-1..]
                |> List.map (fun x -> $"• {x.Text}")
                |> String.concat String.nl
            printer.Warning $"Some inputs were not yet processed: {String.nl}{unprocessedInputs}"

        { NextAction = finalNextAction
          UpdatedSettings = Some finalSettings }

    /// Ensures the download environment is ready, then initiates the input and download process.
    let start (printer: Printer) (settings: UserSettings) : unit =
        // The working directory should start empty. Give the user a chance to empty it.
        match Directories.warnIfAnyFiles 10 settings.WorkingDirectory with
        | Ok () -> ()
        | Error filesFoundErr ->
            printer.Error filesFoundErr
            Directories.askToDeleteAllFiles printer settings.WorkingDirectory |> function
            | Ok results -> Directories.printDeletionResults printer results
            | Error deletionError ->
                printer.Error deletionError
                printer.Info "Aborting..."

        let results = ResultTracker<string> printer
        let history = History(settings.HistoryFile, settings.HistoryDisplayCount)
        let mutable nextAction = Continue
        let mutable currentSettings = settings

        while nextAction = Continue do
            let input = printer.GetInput prompt
            let splitInputs = splitInputText input

            match splitInputs with
            | [] ->
                printer.Error $"Invalid input. Enter only URLs or commands beginning with \"%c{Commands.prefix}\"."
            | _ ->
                let categorizedInputs = categorizeInputs splitInputs
                let categoryCounts = countCategories categorizedInputs
                summarizeInput printer categorizedInputs categoryCounts

                let batchResult = processBatch printer categorizedInputs categoryCounts currentSettings results history
                nextAction <- batchResult.NextAction

                match batchResult.UpdatedSettings with
                | Some newSettings -> currentSettings <- newSettings
                | None -> ()

        results.PrintSessionSummary()
