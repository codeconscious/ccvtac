namespace CCVTAC.Console

open CCVTAC.Console.Downloading
open CCVTAC.Console.InputHelper
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console.Settings.Settings.LiveUpdating
open Spectre.Console
open Startwatch.Library
open System
open System.Threading

module Orchestrator =

    type NextAction =
        | Continue
        | QuitAtUserRequest
        | QuitDueToErrors

    let summarizeInput
        (categorizedInputs: CategorizedInput list)
        (counts: CategoryCounts)
        (printer: Printer)
        : unit =

        if categorizedInputs.Length > 1 then
            let urlCount = counts[InputCategory.Url]
            let cmdCount = counts[InputCategory.Command]

            let urlSummary = match urlCount with 1 -> "1 URL" | n -> $"%d{n} URLs"
            let commandSummary = match cmdCount with 1 -> "1 command" | n -> $"%d{n} commands"
            let conjunction = if String.allHaveText [urlSummary; commandSummary] then " and " else String.Empty
            printer.Info $"Batch of %s{urlSummary}%s{conjunction}%s{commandSummary} entered:"

            for input in categorizedInputs do
                printer.Info $" • %s{input.Text}"

            Printer.EmptyLines 1uy

    let sleep seconds : unit =
        let message seconds = $"Sleeping for {seconds} seconds..."

        let rec loop remaining (ctx: StatusContext) =
            if remaining > 0us then
                ctx.Status (message remaining) |> ignore
                Thread.Sleep 1000
                loop (remaining - 1us) ctx

        AnsiConsole.Status().Start((message seconds), fun ctx ->
            ctx.Spinner(Spinner.Known.Star)
               .SpinnerStyle(Style.Parse "blue")
            |> loop seconds)

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

        match Directories.warnIfAnyFiles 10 settings.WorkingDirectory with
        | Error err ->
            printer.Error err
            Ok NextAction.QuitDueToErrors
        | Ok () ->
            if urlIndex > 1 then // Don't sleep for the first URL.
                sleep settings.SleepSecondsBetweenURLs
                printer.Info(
                    $"{String.newLine}Slept for %d{settings.SleepSecondsBetweenURLs} second(s).",
                    appendLines = 1uy)

            if batchSize > 1 then
                printer.Info $"Processing group %d{urlIndex} of %d{batchSize}..."

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
        if String.equalIgnoringCase Commands.helpCommand command then
            for kvp in Commands.summary do
                printer.Info(kvp.Key)
                printer.Info $"    %s{kvp.Value}"
            Ok NextAction.Continue

        // Quit
        elif List.caseInsensitiveContains command Commands.quitCommands then
            Ok NextAction.QuitAtUserRequest

        // History
        elif List.caseInsensitiveContains command Commands.history then
            history.ShowRecent printer
            Ok NextAction.Continue

        // Update downloader
        elif List.caseInsensitiveContains command Commands.updateDownloader then
            Updater.run settings printer |> ignore
            Ok NextAction.Continue

        // Settings summary
        elif List.caseInsensitiveContains command Commands.settingsSummary then
            Settings.printSummary settings printer None
            Ok NextAction.Continue

        // Toggle split chapters
        elif List.caseInsensitiveContains command Commands.splitChapterToggles then
            settings <- toggleSplitChapters settings
            printer.Info(summarizeToggle "Split Chapters" settings.SplitChapters)
            Ok NextAction.Continue

        // Toggle embed images
        elif List.caseInsensitiveContains command Commands.embedImagesToggles then
            settings <- toggleEmbedImages settings
            printer.Info(summarizeToggle "Embed Images" settings.EmbedImages)
            Ok NextAction.Continue

        // Toggle quiet mode
        elif List.caseInsensitiveContains command Commands.quietModeToggles then
            settings <- toggleQuietMode settings
            printer.Info(summarizeToggle "Quiet Mode" settings.QuietMode)
            printer.ShowDebug(not settings.QuietMode)
            Ok NextAction.Continue

        // Update audio formats
        elif String.startsWithIgnoreCase Commands.updateAudioFormatPrefix command then
            let format = command.Replace(Commands.updateAudioFormatPrefix, String.Empty).ToLowerInvariant()
            if String.hasNoText format then
                Error "You must append one or more supported audio formats separated by commas (e.g., \"m4a,opus,best\")."
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

        // Update audio quality
        elif String.startsWithIgnoreCase Commands.updateAudioQualityPrefix command then
            let inputQuality = command.Replace(Commands.updateAudioQualityPrefix, "")
            if String.hasNoText inputQuality then
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
                    Error $"\"%s{inputQuality}\" is an invalid quality value."

        // Unknown command
        else
            Error (sprintf "\"%s\" is not a valid command. Enter \"%scommands\" to see a list of commands."
                        command
                        (string Commands.prefix))


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
        let watch = Watch()
        let batchResults = ResultTracker<NextAction> printer
        let mutable nextAction = NextAction.Continue
        let mutable inputIndex = 0

        for input in categorizedInputs do
            let mutable stop = false
            inputIndex <- inputIndex + 1

            let result =
                match input.Category with
                | InputCategory.Command ->
                    processCommand input.Text &settings history printer
                | InputCategory.Url ->
                    processUrl input.Text settings resultTracker history inputTime
                               categoryCounts[InputCategory.Url] inputIndex printer

            batchResults.RegisterResult(input.Text, result)

            match result with
            | Error e -> printer.Error e
            | Ok action ->
                nextAction <- action
                if nextAction <> NextAction.Continue then
                    stop <- true

        if categoryCounts[InputCategory.Url] > 1 then
            printer.Info(sprintf "%sFinished with batch of %d URLs in %s."
                            String.newLine
                            categoryCounts[InputCategory.Url]
                            watch.ElapsedFriendly)
            batchResults.PrintBatchFailures()

        nextAction

    /// Ensures the download environment is ready, then initiates the UI input and download process.
    let start (settings: UserSettings) (printer: Printer) : unit =
        // The working directory should start empty. Give the user a chance to empty it.
        match Directories.warnIfAnyFiles 10 settings.WorkingDirectory with
        | Error err ->
            printer.Error err

            match Directories.askToDeleteAllFiles settings.WorkingDirectory printer with
            | Ok deletedCount ->
                printer.Info $"%d{deletedCount} file(s) deleted."
            | Error err ->
                printer.Error err
                printer.Info "Aborting..."
        | Ok () -> ()

        let results = ResultTracker<string> printer
        let history = History(settings.HistoryFile, settings.HistoryDisplayCount)
        let mutable nextAction = NextAction.Continue
        let mutable settingsRef = settings

        while nextAction = NextAction.Continue do
            let input = printer.GetInput prompt
            let splitInputs = splitInputText input

            if List.isEmpty splitInputs then
                printer.Error $"Invalid input. Enter only URLs or commands beginning with \"%c{Commands.prefix}\"."
            else
                let categorizedInputs = categorizeInputs splitInputs
                let categoryCounts = countCategories categorizedInputs
                summarizeInput categorizedInputs categoryCounts printer
                nextAction <- processBatch categorizedInputs categoryCounts &settingsRef results history printer

        results.PrintSessionSummary()

