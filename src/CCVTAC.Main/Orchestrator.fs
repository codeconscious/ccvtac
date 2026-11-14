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
open FSharpPlus
open Startwatch.Library
open System
open System.Globalization

module Orchestrator =

    type NextAction = Continue | QuitAtUserRequest | QuitDueToErrors

    type BatchResults =
        { NextAction: NextAction
          UpdatedSettings: UserSettings option }

    /// Distinguishes input-level failures (skip and continue) from failures
    /// that should halt the batch.
    type ProcessError =
        | Recoverable of string
        | Fatal of string

    type ToggleSpec =
        { Triggers: string list
          Label: string
          Toggle: UserSettings -> UserSettings
          Read: UserSettings -> bool
          AfterToggle: UserSettings -> unit }

    [<Literal>]
    let private maxLeftoverFilesToShow = 10

    let private errorText (error: ProcessError) : string =
        match error with
        | Recoverable msg
        | Fatal msg -> msg

    let summarizeInput
        (printer: Printer)
        (inputs: CategorizedInput list)
        (counts: CategoryCounts)
        : unit =

        if List.hasMultiple inputs then
            let urlSummary = String.plur' "URL" S counts[Url]
            let cmdSummary = String.plur' "command" S counts[Command]

            printer.Info <|
                match counts[Url], counts[Command] with
                | u, c when u > 0 && c > 0 -> $"Batch of %s{urlSummary} and %s{cmdSummary} entered:"
                | u, _ when u > 0 ->          $"Batch of %s{urlSummary} entered:"
                | _, c when c > 0 ->          $"Batch of %s{cmdSummary} entered:"
                | _, _ ->                      "No URLs or commands were entered!"

            for input in inputs do
                printer.Info $" • %s{input.Text}"

            printer.EmptyLine()

    let processUrl
        (printer: Printer)
        (url: string)
        (settings: UserSettings)
        (resultTracker: ResultTracker)
        (history: History)
        (urlInputTime: DateTime)
        (batchSize: int)
        (urlIndex: int)
        : Result<BatchResults, ProcessError> =

        match Directories.warnIfAnyFiles maxLeftoverFilesToShow settings.WorkingDirectory with
        | Error err ->
            Error (Fatal err)
        | Ok () ->
            if batchSize > 1 then
                printer.Info $"Processing item %d{urlIndex} of %d{batchSize}..."

            let jobWatch = Watch()

            match Downloading.mediaTypeWithIds url with
            | Error errMsg ->
                Error (Recoverable $"URL parse error: %s{errMsg}")
            | Ok mediaType ->
                printer.Info $"%s{mediaType.GetType().Name} URL '%s{url}' detected."
                history.Append(url, urlInputTime, printer)

                let downloadResult = Downloader.run printer mediaType settings
                resultTracker.RegisterResult(url, downloadResult)

                match downloadResult with
                | Error errs ->
                    let groupedErrs =
                        errs
                        |> List.map (sprintf "Media download error: %s")
                        |> String.concat String.nl
                    Error (Recoverable groupedErrs)
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

    let summarizeToggle settingName setting : string =
        let state = if setting then "ON" else "OFF"
        $"%s{settingName} toggled to %s{state} for this session."

    let summarizeUpdate settingName setting : string =
        $"%s{settingName} updated to \"%s{setting}\" for this session."

    let private extractValueFromCommand (prefix: string) (command: string) : string =
        command.Substring(prefix.Length).Trim()

    let private toggleSpecs (printer: Printer) : ToggleSpec list =
        [ { Triggers = Commands.splitChapterToggles
            Label = "Split Chapters"
            Toggle = toggleSplitChapters
            Read = _.SplitChapters
            AfterToggle = ignore }

          { Triggers = Commands.embedImagesToggles
            Label = "Embed Images"
            Toggle = toggleEmbedImages
            Read = _.EmbedImages
            AfterToggle = ignore }

          { Triggers = Commands.quietModeToggles
            Label = "Quiet Mode"
            Toggle = toggleQuietMode
            Read = _.QuietMode
            AfterToggle = fun s -> printer.ShowDebug(not s.QuietMode) } ]

    let processCommand printer command settings (history: History) : Result<BatchResults, string> =
        let checkCommand = List.containsIgnoreCase command
        let toggle = toggleSpecs printer |> List.tryFind (fun spec -> checkCommand spec.Triggers)

        match toggle with
        | Some spec ->
            let newSettings = spec.Toggle settings
            printer.Info <| summarizeToggle spec.Label (spec.Read newSettings)
            spec.AfterToggle newSettings
            Ok { NextAction = Continue; UpdatedSettings = Some newSettings }

        | None ->
            if String.equalIgnoreCase Commands.helpCommand command then
                for kvp in Commands.summary do
                    printer.Info kvp.Key
                    printer.Info $"    %s{kvp.Value}"
                Ok { NextAction = Continue; UpdatedSettings = None }

            elif checkCommand Commands.quitCommands then
                Ok { NextAction = QuitAtUserRequest; UpdatedSettings = None }

            elif checkCommand Commands.history then
                history.ShowRecent printer
                Ok { NextAction = Continue; UpdatedSettings = None }

            elif checkCommand Commands.updateDownloader then
                Updater.run printer settings
                Ok { NextAction = Continue; UpdatedSettings = None }

            elif checkCommand Commands.settingsSummary then
                printer.PrintTable (toTable settings)
                Ok { NextAction = Continue; UpdatedSettings = None }

            elif command |> String.startsWithIgnoreCase Commands.updateAudioFormatPrefix then
                let format =
                    command
                    |> extractValueFromCommand Commands.updateAudioFormatPrefix
                    |> _.ToLowerInvariant()

                if String.hasNoText format then
                    Error "Append one or more supported audio formats separated by commas (e.g., \"m4a,opus,best\")."
                else
                    match updateAudioFormat settings format with
                    | Error err ->
                        Error err
                    | Ok newSettings ->
                        let audioFormats = String.Join(", ", newSettings.AudioFormats)
                        printer.Info <| summarizeUpdate "Audio Formats" audioFormats
                        Ok { NextAction = Continue; UpdatedSettings = Some newSettings }

            elif command |> String.startsWithIgnoreCase Commands.updateAudioQualityPrefix then
                let inputQuality = extractValueFromCommand Commands.updateAudioQualityPrefix command
                let limit = 10uy
                let rangeMsg = $"Audio quality must be between %d{limit} (lowest) and 0 (highest)."

                if String.hasNoText inputQuality then
                    Error rangeMsg
                else
                    match Byte.TryParse(inputQuality, NumberStyles.None, CultureInfo.InvariantCulture) with
                    | true, quality when quality <= limit ->
                        updateAudioQuality settings quality
                        |> Result.map (fun newSettings ->
                            printer.Info <| summarizeUpdate "Audio Quality" (newSettings.AudioQuality.ToString())
                            { NextAction = Continue; UpdatedSettings = Some newSettings })
                    | true, _ ->
                        Error rangeMsg
                    | _ ->
                        Error $"\"%s{inputQuality}\" is an invalid quality value. %s{rangeMsg}"

            else
                Error $"\"%s{command}\" is not a valid command. Enter \"%c{Commands.prefix}help\" to see a list of commands."

    /// Processes a single user request, from input to downloading and file post-processing.
    let processBatch
        (printer: Printer)
        (inputs: CategorizedInput list)
        (categoryCounts: CategoryCounts)
        (settings: UserSettings)
        (resultTracker: ResultTracker)
        (history: History)
        : BatchResults =

        let inputTime = DateTime.Now
        let batchWatch = Watch()

        let printSleep currentSettings (inputCategory: InputCategory) =
            if inputCategory.IsUrl then
                let seconds = currentSettings.SleepSecondsBetweenURLs
                let secondsMsg secs = String.plur' "second" S secs
                let sleptMsg =
                    sleep
                        (fun remainingSeconds -> $"Sleeping for {secondsMsg remainingSeconds}...")
                        (fun totalSeconds -> $"Slept for {secondsMsg totalSeconds}.")
                        seconds
                printer.Info $"{String.nl}{sleptMsg}"
                printer.EmptyLine()

        let processInput currentSettings inputCategory text index : Result<BatchResults, ProcessError> =
            match inputCategory with
            | Command ->
                processCommand printer text currentSettings history
                |> Result.mapError Recoverable
            | Url ->
                processUrl printer text currentSettings resultTracker history inputTime inputs.Length index

        let deleteLeftoverFiles dirName : Result<string option, string> =
            match Directories.warnIfAnyFiles maxLeftoverFilesToShow dirName with
            | Ok () -> Ok None
            | Error msg ->
                printer.Warning msg
                Directories.deleteAllFiles dirName
                |> Result.map (fun results ->
                    Directories.printDeletionResults printer results
                    Some "Files deleted successfully.")
                |> Result.mapError (sprintf "Error deleting leftover files after download: %s")

        let rec loop inputs settings nextAction' index : NextAction * UserSettings * int =
            match inputs with
            | input :: remainingInputs when nextAction' = Continue ->
                let processResult = processInput settings input.Category input.Text index

                resultTracker.RegisterResult(
                    input.Text,
                    processResult |> Result.mapError (fun err -> List.cons (errorText err) []))

                processResult |> Result.iterError (errorText >> printer.Error)

                match processResult with
                | Error (Fatal _) ->
                    // The item was not processed, so `index` (not index + 1) keeps it in the unprocessed list.
                    (QuitDueToErrors, settings, index)
                | _ ->
                    let cleanupResult =
                        if input.Category.IsUrl
                        then deleteLeftoverFiles settings.WorkingDirectory
                        else Ok None

                    match cleanupResult with
                    | Error errMsg ->
                        printer.Error errMsg
                        (QuitDueToErrors, settings, index + 1)
                    | Ok message ->
                        message |> Option.iter printer.Debug
                        if List.isNotEmpty remainingInputs then printSleep settings input.Category

                        let updatedSettings, nextAction =
                            match processResult with
                            | Ok result ->
                                defaultArg result.UpdatedSettings settings, result.NextAction
                            | Error _ ->
                                settings, nextAction'

                        loop remainingInputs updatedSettings nextAction (index + 1)
            | _ ->
                (nextAction', settings, index)

        let (finalNextAction, finalSettings, processedCount) =
            loop inputs settings Continue 1

        if categoryCounts[Url] > 1 then
            printer.Info(
                sprintf "%sFinished with batch of %d URLs in %s."
                    String.nl
                    categoryCounts[Url]
                    batchWatch.ElapsedFriendly
                )
            resultTracker.PrintBatchFailures()

        if processedCount <= inputs.Length then
            let unprocessedInputs =
                inputs[processedCount-1..]
                |> List.map (fun x -> $"• {x.Text}")
                |> String.concat String.nl
            printer.Warning $"Some inputs were not yet processed: {String.nl}{unprocessedInputs}"

        { NextAction = finalNextAction
          UpdatedSettings = Some finalSettings }

    let private isWorkingDirectoryReady (printer: Printer) dirName : bool =
        match Directories.warnIfAnyFiles maxLeftoverFilesToShow dirName with
        | Ok () ->
            true
        | Error err ->
            printer.Error err
            match Directories.askToDeleteAllFiles printer dirName with
            | Ok results ->
                Directories.printDeletionResults printer results
                true
            | Error deletionError ->
                printer.Error deletionError
                printer.Info "Aborting..."
                false

    let start printer settings : NextAction =
        if not (isWorkingDirectoryReady printer settings.WorkingDirectory) then
            QuitDueToErrors
        else
            let results = ResultTracker printer
            let history = History(settings.HistoryFile, settings.HistoryDisplayCount)

            let rec repl (currentSettings: UserSettings) : NextAction =
                let input = printer.GetInput prompt
                match splitInputText input with
                | [] ->
                    printer.Error $"Invalid input. Enter only URLs or commands beginning with \"%c{Commands.prefix}\"."
                    repl currentSettings
                | splitInputs ->
                    let categorizedInputs = categorizeInputs splitInputs
                    let categoryCounts = countCategories categorizedInputs
                    summarizeInput printer categorizedInputs categoryCounts

                    let batchResult =
                        processBatch printer categorizedInputs categoryCounts currentSettings results history

                    let nextSettings = defaultArg batchResult.UpdatedSettings currentSettings

                    match batchResult.NextAction with
                    | Continue -> repl nextSettings
                    | action ->
                        results.PrintSessionSummary()
                        action

            repl settings
