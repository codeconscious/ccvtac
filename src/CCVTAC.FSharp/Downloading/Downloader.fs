namespace CCVTAC.Console.Downloading

open CCVTAC.Console
open CCVTAC.Console.ExternalTools.Runner
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.IoUtilities.Directories
open CCVTAC.Console.Downloading.Downloading
open CCVTAC.Console.ExternalTools
open CCVTAC.Console.Settings.Settings
open System

module Downloader =

    [<Literal>]
    let private programName = "yt-dlp"

    type Urls = { Primary: string
                  Supplementary: string option }

    // TODO: Is the audioFormat not in the settings?
    /// Generate the entire argument string for the download tool.
    /// audioFormat: one of the supported audio format codes (or null for none)
    /// mediaType: Some MediaType for normal downloads, None for metadata-only supplementary downloads
    /// additionalArgs: optional extra args (e.g., the URL)
    let generateDownloadArgs audioFormat settings (mediaType: MediaType option) additionalArgs : string =
        let writeJsonArg = "--write-info-json"
        let trimFileNamesArg = "--trim-filenames 250"

        let formatArg =
            match audioFormat with
            | None -> String.Empty
            | Some f when f = "best" -> String.Empty
            | Some f -> $"-f {f}"

        let mutable args =
            match mediaType with
            | None ->
                [ $"--flat-playlist {writeJsonArg} {trimFileNamesArg}" ]
            | Some _ ->
                [ $"--extract-audio {formatArg}"
                  $"--audio-quality {settings.AudioQuality}"
                  "--write-thumbnail --convert-thumbnails jpg"
                  writeJsonArg
                  trimFileNamesArg
                  "--retries 2" ]
            |> Set.ofList

        if settings.QuietMode then
            args <- args.Add "--quiet --no-warnings"

        // No MediaType indicates that this is a supplemental metadata-only download.
        match mediaType with
        | Some mt ->
            if settings.SplitChapters then
                args <- args.Add "--split-chapters"

            if not mt.IsVideo && not mt.IsPlaylistVideo then
                args <- args.Add $"--sleep-interval {settings.SleepSecondsBetweenDownloads}"

            if mt.IsStandardPlaylist then
                args <- args.Add
                    """-o "%(uploader).80B - %(playlist).80B - %(playlist_autonumber)s - %(title).150B [%(id)s].%(ext)s" --playlist-reverse"""
        | None -> ()

        let extraArgs = defaultArg additionalArgs [] |> Set.ofList
        String.Join(" ", Set.union args extraArgs)

    let wrapUrlInMediaType url : Result<MediaType, string> =
        mediaTypeWithIds url

    /// Completes the actual download process.
    /// Returns a Result that, if successful, contains the name of the successfully downloaded format.
    let run (mediaType: MediaType) userSettings (printer: Printer) : Result<string, string> =
        if not mediaType.IsVideo && not mediaType.IsPlaylistVideo then
            printer.Info("Please wait for multiple videos to be downloaded...")

        let rawUrls = generateDownloadUrl(mediaType)

        let urls =
            { Primary = rawUrls[0]
              Supplementary = if rawUrls.Length = 2 then Some rawUrls[1] else None }

        let mutable downloadResult : Result<ToolResult, string> = Error String.Empty
        let mutable successfulFormat = String.Empty
        let mutable stopped = false

        for format in userSettings.AudioFormats do
            if not stopped then
                let args = generateDownloadArgs (Some format) userSettings (Some mediaType) (Some [urls.Primary])
                let commandWithArgs = $"{programName} {args}"
                let downloadSettings = ToolSettings.create commandWithArgs userSettings.WorkingDirectory

                downloadResult <- runTool downloadSettings [1] printer

                match downloadResult with
                | Ok result ->
                    successfulFormat <- format

                    if result.ExitCode <> 0 then
                        printer.Warning "Downloading completed with minor issues."
                        match result.Error with
                        | Some w -> printer.Warning w
                        | None -> ()

                    stopped <- true
                | Error e ->
                    printer.Debug $"Failure downloading \"%s{format}\" format: %s{e}"

        let mutable errors = match downloadResult with Error err -> [err] | Ok _ -> []

        if audioFileCount userSettings.WorkingDirectory Files.audioFileExtensions = 0 then
            "No audio files were downloaded." :: errors
            |> String.concat String.newLine
            |> Error
        else
            // Continue to post-processing if errors.
            if List.isNotEmpty errors then
                errors |> List.iter printer.Error
                printer.Info "Post-processing will still be attempted."
            else
                // Attempt a metadata-only supplementary download.
                match urls.Supplementary with
                | Some supplementaryUrl ->
                    let args = generateDownloadArgs None userSettings None (Some [supplementaryUrl])
                    let commandWithArgs = $"{programName} {args}"
                    let downloadSettings = ToolSettings.create commandWithArgs userSettings.WorkingDirectory
                    let supplementaryDownloadResult = runTool downloadSettings [1] printer

                    match supplementaryDownloadResult with
                    | Ok _ ->
                        printer.Info "Supplementary metadata download completed OK."
                    | Error err ->
                        printer.Error "Supplementary metadata download failed."
                        errors <- List.append [err] errors
                | None -> ()

            if List.isEmpty errors then
                Ok successfulFormat
            else
                Error (String.Join(" / ", errors))



