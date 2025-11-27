namespace CCVTAC.Console.Downloading

open CCVTAC.Console
open CCVTAC.Console.IoUtilities.Directories
open CCVTAC.Console.Downloading.Downloading
open CCVTAC.Console.ExternalTools
open CCVTAC.Console.Settings.Settings
open System

module Downloader =

    [<Literal>]
    let private ProgramName = "yt-dlp"

    type Urls = { Primary: string
                  Supplementary: string option }

    // TODO: Is the audioFormat not in the settings?
    /// Generate the entire argument string for the download tool.
    /// audioFormat: one of the supported audio format codes (or null for none)
    /// mediaType: Some MediaType for normal downloads, None for metadata-only supplementary downloads
    /// additionalArgs: optional extra args (e.g., the URL)
    let generateDownloadArgs audioFormat settings (mediaType: MediaType option) additionalArgs : string =
        let writeJson = "--write-info-json"
        let trimFileNames = "--trim-filenames 250"

        let formatArg =
            match audioFormat with
            | None -> String.Empty
            | Some f when f = "best" -> String.Empty
            | Some f -> $"-f {f}"

        let args =
            match mediaType with
            | None ->
                [ $"--flat-playlist {writeJson} {trimFileNames}" ]
            | Some _ ->
                [ "--extract-audio"
                  formatArg
                  $"--audio-quality {settings.AudioQuality}"
                  "--write-thumbnail --convert-thumbnails jpg"
                  writeJson
                  trimFileNames
                  "--retries 2" ]
            |> Set.ofList

        if settings.QuietMode then
            args.Add "--quiet --no-warnings" |> ignore

        match mediaType with
        | Some mt ->
            if settings.SplitChapters then
                args.Add("--split-chapters") |> ignore

            if not mt.IsVideo && not mt.IsPlaylistVideo then
                args.Add($"--sleep-interval {settings.SleepSecondsBetweenDownloads}") |> ignore

            if mt.IsStandardPlaylist then
                args.Add
                    """-o "%(uploader).80B - %(playlist).80B - %(playlist_autonumber)s - %(title).150B [%(id)s].%(ext)s" --playlist-reverse"""
                |> ignore
        | None -> ()

        let extras = defaultArg additionalArgs [] |> Set.ofList
        String.Join(" ", args |> Set.union extras)

    let internal wrapUrlInMediaType url : Result<MediaType, string> =
        mediaTypeWithIds url

    /// Completes the actual download process.
    /// Returns a Result that, if successful, contains the name of the successfully downloaded format.
    let internal run (mediaType: MediaType) settings (printer: Printer) : Result<string, string> =
        if not mediaType.IsVideo && not mediaType.IsPlaylistVideo then
            printer.Info("Please wait for multiple videos to be downloaded...")

        let rawUrls = extractDownloadUrls(mediaType)
        let urls =
            { Primary = rawUrls[0]
              Supplementary = if rawUrls.Length = 2 then Some rawUrls[1] else None }

        let mutable downloadResult : Result<int * string option, string> = Error String.Empty
        let mutable successfulFormat = String.Empty
        let mutable stopped = false
        let mutable errors : string list = []

        for format in settings.AudioFormats do
            if not stopped then
                let args = generateDownloadArgs (Some format) settings (Some mediaType) (Some [urls.Primary])
                let commandWithArgs = $"{ProgramName} {args}"
                let downloadSettings = ToolSettings.create commandWithArgs settings.WorkingDirectory

                downloadResult <- Runner.run downloadSettings [1] printer

                match downloadResult with
                | Ok (exitCode, warning) ->
                    successfulFormat <- format

                    if exitCode <> 0 then
                        printer.Warning "Downloading completed with minor issues."
                        match warning with
                        | Some w -> printer.Warning w
                        | None -> ()

                    stopped <- true
                | Error e ->
                    printer.Debug $"Failure downloading \"%s{format}\" format: %s{e}"

        errors <- match downloadResult with Error e -> [e] | Ok _ -> []

        if audioFileCount settings.WorkingDirectory = 0 then
            let combinedErrors =
                errors
                |> List.append ["No audio files were downloaded."]
                |> String.concat newLine
            Error combinedErrors
        else
            // Continue to post-processing if errors.
            if List.isNotEmpty errors then
                errors |> List.iter printer.Error
                printer.Info "Post-processing will still be attempted."
            else
                // Attempt a metadata-only supplementary download.
                match urls.Supplementary with
                | Some supplementaryUrl ->
                    let args = generateDownloadArgs None settings None (Some [supplementaryUrl])
                    let commandWithArgs = $"{ProgramName} {args}"
                    let downloadSettings = ToolSettings.create commandWithArgs settings.WorkingDirectory
                    let supplementaryDownloadResult = Runner.run downloadSettings [1] printer

                    match supplementaryDownloadResult with
                    | Ok _ ->
                        printer.Info("Supplementary metadata download completed OK.")
                    | Error err ->
                        printer.Error("Supplementary metadata download failed.")
                        errors <- List.append [err] errors
                | None -> ()

            if List.isEmpty errors then
                Ok successfulFormat
            else
                Error (String.Join(" / ", errors))



