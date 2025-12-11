namespace CCVTAC.Console.Downloading

open CCVTAC.Console
open CCVTAC.Console.ExternalTools.Runner
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.IoUtilities.Directories
open CCVTAC.Console.Downloading.Downloading
open CCVTAC.Console.ExternalTools
open CCVTAC.Console.Settings.Settings
open FsToolkit.ErrorHandling
open System

module Downloader =

    [<Literal>]
    let private programName = "yt-dlp"

    type PrimaryUrl = PrimaryUrl of string
    type SupplementaryUrl = SupplementaryUrl of string option

    type Urls = { Primary: PrimaryUrl
                  Metadata: SupplementaryUrl }

    // TODO: Is the audioFormat not in the settings?
    /// Generate the entire argument string for the download tool.
    /// audioFormat: one of the supported audio format codes (or null for none)
    /// mediaType: Some MediaType for normal downloads, None for metadata-only supplementary downloads
    /// additionalArgs: optional extra args (e.g., the URL)
    let generateDownloadArgs audioFormat userSettings (mediaType: MediaType option) additionalArgs : string =
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
                  $"--audio-quality {userSettings.AudioQuality}"
                  "--write-thumbnail --convert-thumbnails jpg"
                  writeJsonArg
                  trimFileNamesArg
                  "--retries 2" ]
            |> Set.ofList

        if userSettings.QuietMode then
            args <- args.Add "--quiet --no-warnings"

        // No MediaType indicates that this is a supplemental metadata-only download.
        // TODO: Add a union type to more clearly indicate this difference.
        match mediaType with
        | Some mt ->
            if userSettings.SplitChapters then
                args <- args.Add "--split-chapters"

            if not mt.IsVideo && not mt.IsPlaylistVideo then
                args <- args.Add $"--sleep-interval {userSettings.SleepSecondsBetweenDownloads}"

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
    let downloadMedia (printer: Printer) (mediaType: MediaType) userSettings (PrimaryUrl url)
        : Result<string list, string list> =

        if not mediaType.IsVideo && not mediaType.IsPlaylistVideo then
            printer.Info("Please wait for multiple videos to be downloaded...")

        let rec loop (errors: string list) formats =
            match formats with
            | [] ->
                Error errors
            | format :: fs ->
                let args = generateDownloadArgs (Some format) userSettings (Some mediaType) (Some [url])
                let commandWithArgs = $"{programName} {args}"
                let downloadSettings = ToolSettings.create commandWithArgs userSettings.WorkingDirectory

                let downloadResult = runTool downloadSettings [1] printer
                let filesDownloaded = audioFileCount userSettings.WorkingDirectory Files.audioFileExtensions > 0

                match downloadResult, filesDownloaded with
                | Ok result, true ->
                    Ok (List.append [$"Successfully downloaded the \"{format}\" format."] errors)
                | Ok result, false ->
                    Error (List.append errors [$"The downloader reported OK for \"{format}\", but no audio files were downloaded."])
                | Error err, true ->
                    Error (List.append errors [$"Error was reported for \"{format}\", but audio files were unexpectedly found."])
                | Error err, false ->
                    loop (List.append errors [$"Error was reported for \"{format}\", and no audio files were downloaded."]) fs

        loop [] userSettings.AudioFormats

    let downloadMetadata (printer: Printer) (mediaType: MediaType) userSettings (SupplementaryUrl url)
        : Result<string list, string list> =

        match url with
        | Some url' ->
            let args = generateDownloadArgs None userSettings None (Some [url'])
            let commandWithArgs = $"{programName} {args}"
            let downloadSettings = ToolSettings.create commandWithArgs userSettings.WorkingDirectory
            let supplementaryDownloadResult = runTool downloadSettings [1] printer

            match supplementaryDownloadResult with
            | Ok _ -> Error ["Supplementary metadata download completed OK."]
            | Error err -> Error [$"Supplementary metadata download failed: {err}"]
        | None -> Ok ["No supplementary link found."]

    /// Completes the actual download process.
    /// Returns a Result that, if successful, contains the name of the successfully downloaded format.
    let run (mediaType: MediaType) userSettings (printer: Printer) : Result<string list, string list> =
        result {
            let rawUrls = generateDownloadUrl mediaType
            let urls =
                { Primary = PrimaryUrl rawUrls[0]
                  Metadata = SupplementaryUrl <| if rawUrls.Length = 2 then Some rawUrls[1] else None }
            let! mediaDownloadResult = downloadMedia printer mediaType userSettings urls.Primary
            let! metadataDownloadResult = downloadMetadata printer mediaType userSettings urls.Metadata
            return! Ok metadataDownloadResult
        }
