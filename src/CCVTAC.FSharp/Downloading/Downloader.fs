namespace CCVTAC.Console.Downloading

open CCVTAC.Console
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console.ExternalTools
open System
open System.Collections.Generic
open System.Linq
open CCVTAC.Console.ExternalTools
open CCVTAC.Console.Downloading.Downloading
// open CCVTAC.FSharp.Downloading
// open CCVTAC.Console.Settings.UserSettings

module Downloader =

    [<Literal>]
    let private ProgramName = "yt-dlp"

    type Urls = { Primary: string; Supplementary: string option }

    /// Generate the entire argument string for the download tool.
    /// audioFormat: one of the supported audio format codes (or null for none)
    /// mediaType: Some MediaType for normal downloads, None for metadata-only supplementary downloads
    /// additionalArgs: optional extra args (e.g., the URL)
    let GenerateDownloadArgs
        (audioFormat: string option)
        (settings: UserSettings)
        (mediaType: MediaType option)
        (additionalArgs: string[] option)
        : string =

        let writeJson = "--write-info-json"
        let trimFileNames = "--trim-filenames 250"

        let formatArg =
            // if String.IsNullOrWhiteSpace audioFormat || audioFormat = "best" then
            //     String.Empty
            // else
            //     $"-f {audioFormat}"
            match audioFormat with
            | None -> String.Empty
            | Some format when format = "best" -> String.Empty
            | Some format -> $"-f {format}"

        let args =
            match mediaType with
            | None ->
                HashSet<string>([ $"--flat-playlist {writeJson} {trimFileNames}" ])
            | Some _ ->
                HashSet<string>(
                    [
                        "--extract-audio"
                        formatArg
                        $"--audio-quality {settings.AudioQuality}"
                        "--write-thumbnail --convert-thumbnails jpg"
                        writeJson
                        trimFileNames
                        "--retries 2"
                    ]
                )

        // quiet vs verbose
        args.Add(if settings.QuietMode then "--quiet --no-warnings" else String.Empty) |> ignore

        match mediaType with
        | Some mt ->
            if settings.SplitChapters then args.Add("--split-chapters") |> ignore

            if not mt.IsVideo && not mt.IsPlaylistVideo then
                args.Add($"--sleep-interval {settings.SleepSecondsBetweenDownloads}") |> ignore

            if mt.IsStandardPlaylist then
                args.Add(
                    """-o "%(uploader).80B - %(playlist).80B - %(playlist_autonumber)s - %(title).150B [%(id)s].%(ext)s" --playlist-reverse"""
                ) |> ignore
        | None -> ()

        let extras = defaultArg additionalArgs [||]
        String.Join(" ", args.Concat(extras))

    let internal WrapUrlInMediaType (url: string) : Result<MediaType, string> =
        mediaTypeWithIds url
        // if result.IsOk then Result.Ok result.ResultValue else Result.Fail result.ErrorValue

    /// Completes the actual download process.
    /// Returns a Result that, if successful, contains the name of the successfully downloaded format.
    let internal Run
        (mediaType: MediaType)
        (settings: UserSettings)
        (printer: Printer)
        : Result<string, string> =

        if not mediaType.IsVideo && not mediaType.IsPlaylistVideo then
            printer.Info("Please wait for multiple videos to be downloaded...")

        let rawUrls = extractDownloadUrls(mediaType)
        let urls =
            { Primary = rawUrls.[0]
              Supplementary = if rawUrls.Length = 2 then Some rawUrls.[1] else None }

        // Placeholder for the download result; we'll overwrite as we try formats.
        let mutable downloadResult : Result<int * string, string> = Error ""
        let mutable successfulFormat : string = ""

        let mutable stopped = false
        for format in settings.AudioFormats do
            if not stopped then
                let args = GenerateDownloadArgs (Some format) settings (Some mediaType) (Some [| urls.Primary |])
                let commandWithArgs = $"{ProgramName} {args}"
                let downloadSettings = ToolSettings.create commandWithArgs settings.WorkingDirectory

                let downloadResult = Runner.run downloadSettings [| 1 |] printer

                match downloadResult with
                | Ok (exitCode, warning) ->
                    successfulFormat <- format

                    if exitCode <> 0 then
                        printer.Warning("Downloading completed with minor issues.")
                        if not (String.IsNullOrWhiteSpace warning) then
                            printer.Warning(warning)

                    stopped <- true
                | Error e ->
                    printer.Debug(sprintf "Failure downloading \"%s\" format." format)

        // Collect error messages from the last download attempt
        let mutable errors = match downloadResult with Error e -> [e] | Ok _ -> []

        let audioFileCount = IoUtilities.Directories.audioFileCount settings.WorkingDirectory
        if audioFileCount = 0 then
            let combined =
                errors
                |> Seq.append (seq { yield "No audio files were downloaded." })
                |> String.concat Environment.NewLine
            Error combined
        else
            // If there were errors from the primary download attempts, print them and continue to post-processing.
            if errors.Length <> 0 then
                errors |> List.iter printer.Error
                printer.Info("Post-processing will still be attempted.")
            else
                // If no errors and there is a supplementary URL, attempt a metadata-only supplementary download.
                match urls.Supplementary with
                | Some supp ->
                    let supplementaryArgs = GenerateDownloadArgs None settings None (Some [| supp |])
                    let commandWithArgs = $"{ProgramName} {supplementaryArgs}"
                    let supplementaryDownloadSettings = ToolSettings.create commandWithArgs settings.WorkingDirectory

                    let supplementaryDownloadResult =
                        Runner.run supplementaryDownloadSettings [| 1 |] printer

                    // if supplementaryDownloadResult.IsSuccess then
                    //     printer.Info("Supplementary download completed OK.")
                    // else
                    //     printer.Error("Supplementary download failed.")
                    //     errors.AddRange(supplementaryDownloadResult.Errors.Select(fun e -> e.Message))
                    match supplementaryDownloadResult with
                    | Ok _ ->
                        printer.Info("Supplementary download completed OK.")
                    | Error e ->
                        printer.Error("Supplementary download failed.")
                        errors <- errors |> List.append [e]
                | None -> ()

            if errors.Length > 0 then
                Error (String.Join(" / ", errors))
            else
                Ok successfulFormat



