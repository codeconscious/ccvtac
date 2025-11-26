namespace CCVTAC.Console.Settings

open System
open System.Text.Json.Serialization
open CCVTAC.Console
open Spectre.Console

module Settings =

    type FilePath = FilePath of string

    type RenamePattern = {
        [<JsonPropertyName("regex")>]           RegexPattern : string
        [<JsonPropertyName("replacePattern")>]  ReplaceWithPattern : string
        [<JsonPropertyName("summary")>]         Summary : string
    }

    type TagDetectionPattern = {
        [<JsonPropertyName("regex")>]        RegexPattern : string
        [<JsonPropertyName("matchGroup")>]   MatchGroup : int
        [<JsonPropertyName("searchField")>]  SearchField : string
        [<JsonPropertyName("summary")>]      Summary : string option
    }

    type TagDetectionPatterns = {
        [<JsonPropertyName("title")>]     Title : TagDetectionPattern array
        [<JsonPropertyName("artist")>]    Artist : TagDetectionPattern array
        [<JsonPropertyName("album")>]     Album : TagDetectionPattern array
        [<JsonPropertyName("composer")>]  Composer : TagDetectionPattern array
        [<JsonPropertyName("year")>]      Year : TagDetectionPattern array
    }

    type UserSettings = {
        [<JsonPropertyName("workingDirectory")>]              WorkingDirectory: string
        [<JsonPropertyName("moveToDirectory")>]               MoveToDirectory: string
        [<JsonPropertyName("historyFile")>]                   HistoryFile: string
        [<JsonPropertyName("historyDisplayCount")>]           HistoryDisplayCount: byte
        [<JsonPropertyName("audioFormats")>]                  AudioFormats: string array
        [<JsonPropertyName("audioQuality")>]                  AudioQuality: byte
        [<JsonPropertyName("splitChapters")>]                 SplitChapters: bool
        [<JsonPropertyName("sleepSecondsBetweenDownloads")>]  SleepSecondsBetweenDownloads: uint16
        [<JsonPropertyName("sleepSecondsBetweenURLs")>]       SleepSecondsBetweenURLs: uint16
        [<JsonPropertyName("quietMode")>]                     QuietMode: bool
        [<JsonPropertyName("embedImages")>]                   EmbedImages: bool
        [<JsonPropertyName("doNotEmbedImageUploaders")>]      DoNotEmbedImageUploaders: string array
        [<JsonPropertyName("ignoreUploadYearUploaders")>]     IgnoreUploadYearUploaders: string array
        [<JsonPropertyName("tagDetectionPatterns")>]          TagDetectionPatterns: TagDetectionPatterns
        [<JsonPropertyName("renamePatterns")>]                RenamePatterns: RenamePattern array
        [<JsonPropertyName("normalizationForm")>]             NormalizationForm : string
        [<JsonPropertyName("downloaderUpdateCommand")>]       DownloaderUpdateCommand : string
    }

    let summarize settings : (string * string) list =
        let onOrOff = function
            | true -> "ON"
            | false -> "OFF"

        let pluralize (label: string) count =
            if count = 1
            then $"{count} {label}"
            else $"{count} {label}s" // Intentionally naive implementation.

        let tagDetectionPatternCount (patterns: TagDetectionPatterns) =
            patterns.Title.Length +
            patterns.Artist.Length +
            patterns.Album.Length +
            patterns.Composer.Length +
            patterns.Year.Length

        [
            ("Working directory", settings.WorkingDirectory)
            ("Move-to directory", settings.MoveToDirectory)
            ("History log file", settings.HistoryFile)
            ("Split video chapters", onOrOff settings.SplitChapters)
            ("Embed images", onOrOff settings.EmbedImages)
            ("Quiet mode", onOrOff settings.QuietMode)
            ("Audio formats", String.Join(", ", settings.AudioFormats))
            ("Audio quality (10 up to 0)", settings.AudioQuality |> sprintf "%B")
            ("Sleep between URLs", settings.SleepSecondsBetweenURLs |> int |> pluralize "second")
            ("Sleep between downloads", settings.SleepSecondsBetweenDownloads |> int |> pluralize "second")
            ("Ignore-upload-year channels", settings.IgnoreUploadYearUploaders.Length |> pluralize "channel")
            ("Do-not-embed-image channels", settings.DoNotEmbedImageUploaders.Length |> pluralize "channel")
            ("Tag-detection patterns", tagDetectionPatternCount settings.TagDetectionPatterns |> pluralize "pattern")
            ("Rename patterns", settings.RenamePatterns.Length |> pluralize "pattern")
        ]

    let printSummary settings (printer: Printer) headerOpt : unit =
        match headerOpt with
        | Some h when hasNonWhitespaceText h -> printer.Info h
        | _ -> ()

        let table = Table()
        table.Expand() |> ignore
        table.Border <- TableBorder.HeavyEdge
        table.BorderColor(Color.Grey27) |> ignore
        table.AddColumns("Name", "Value") |> ignore
        table.HideHeaders() |> ignore
        table.Columns[1].Width <- 100 // Ensure maximum width.

        for description, value in summarize settings do
            table.AddRow(description, value) |> ignore

        Printer.PrintTable table

    module Validation =
        open System.IO

        let validate settings =
            let isEmpty str = String.IsNullOrWhiteSpace str
            let dirMissing str = not (Directory.Exists str)

            // Source: https://github.com/yt-dlp/yt-dlp/?tab=readme-ov-file#post-processing-options
            let supportedAudioFormats = [ "best"; "aac"; "alac"; "flac"; "m4a"; "mp3"; "opus"; "vorbis"; "wav" ]
            let supportedNormalizationForms = [ "C"; "D"; "KC"; "KD" ]

            let validAudioFormat fmt = supportedAudioFormats |> List.contains fmt

            match settings with
            | { WorkingDirectory = dir } when isEmpty dir ->
                Error "No working directory was specified."
            | { WorkingDirectory = dir } when dirMissing dir ->
                Error $"Working directory \"{dir}\" is missing."
            | { MoveToDirectory = dir } when isEmpty dir ->
                Error "No move-to directory was specified."
            | { MoveToDirectory = dir } when dirMissing dir ->
                Error $"Move-to directory \"{dir}\" is missing."
            | { AudioQuality = q } when q > 10uy ->
                Error "Audio quality must be in the range 10 (lowest) and 0 (highest)."
            | { NormalizationForm = nf } when not(supportedNormalizationForms |> List.contains (nf.ToUpperInvariant())) ->
                let okFormats = String.Join(", ", supportedNormalizationForms)
                Error $"\"{nf}\" is an invalid normalization form. Use one of the following: {okFormats}."
            | { AudioFormats = fmt } when not (fmt |> Array.forall (fun f -> f |> validAudioFormat)) ->
                let formats = String.Join(", ", fmt)
                let approved = supportedAudioFormats |> String.concat ", "
                let nl = Environment.NewLine
                Error $"Audio formats (\"%s{formats}\") include an unsupported audio format.{nl}Only the following supported formats: {approved}."
            | _ ->
                Ok settings

    module IO =
        open System.IO
        open System.Text.Json
        open System.Text.Unicode
        open System.Text.Encodings.Web
        open Validation

        let deserialize<'a> (json: string) : Result<'a, string> =
            let options = JsonSerializerOptions()
            options.AllowTrailingCommas <- true
            options.ReadCommentHandling <- JsonCommentHandling.Skip
            match JsonSerializer.Deserialize<'a>(json, options) with // TODO: Add exception handling.
            | null -> Error "Could not deserialize the JSON"
            | s -> Ok s

        let fileExists (FilePath path) =
            match path |> File.Exists with
            | true -> Ok()
            | false -> Error $"File \"{path}\" does not exist."

        let read (FilePath path) =
            try
                path
                |> File.ReadAllText
                |> deserialize<UserSettings>
                |> Result.bind validate
            with
                | :? FileNotFoundException -> Error $"File \"{path}\" was not found."
                | :? JsonException as e -> Error $"Parse error in \"{path}\": {e.Message}"
                | e -> Error $"Unexpected error reading from \"{path}\": {e.Message}"

        let private writeFile (FilePath file) settings =
            let unicodeEncoder = JavaScriptEncoder.Create UnicodeRanges.All
            let writeIndented = true
            let options = JsonSerializerOptions(WriteIndented = writeIndented, Encoder = unicodeEncoder)

            try
                let json = JsonSerializer.Serialize(settings, options)
                (file, json) |> File.WriteAllText
                Ok $"A new settings file was saved to \"{file}\". Please populate it with your desired settings."
            with
                | :? FileNotFoundException -> Error $"File \"{file}\" was not found."
                | :? JsonException -> Error "Failure parsing user settings to JSON."
                | e -> Error $"Failure writing \"{file}\": {e.Message}"

        let writeDefaultFile (filePath: FilePath option) defaultFileName =
            let confirmedPath =
                match filePath with
                | Some p -> p
                | None -> FilePath (Path.Combine(AppContext.BaseDirectory, defaultFileName))

            let defaultSettings =
                {
                    WorkingDirectory = String.Empty
                    MoveToDirectory = String.Empty
                    HistoryFile = String.Empty
                    HistoryDisplayCount = 25uy // byte
                    SplitChapters = true
                    SleepSecondsBetweenDownloads = 10us
                    SleepSecondsBetweenURLs = 15us
                    AudioFormats = [||]
                    AudioQuality = 0uy
                    QuietMode = false
                    EmbedImages = true
                    DoNotEmbedImageUploaders = [||]
                    IgnoreUploadYearUploaders = [||]
                    TagDetectionPatterns = {
                        Title = [||]
                        Artist = [||]
                        Album = [||]
                        Composer = [||]
                        Year = [||]
                    }
                    RenamePatterns = [||]
                    NormalizationForm = "C" // Recommended for compatibility between Linux and macOS.
                    DownloaderUpdateCommand = String.Empty
                }

            writeFile confirmedPath defaultSettings

    module LiveUpdating =
        open Validation

        let toggleSplitChapters settings =
            let toggledSetting = not settings.SplitChapters
            { settings with SplitChapters = toggledSetting }

        let toggleEmbedImages settings =
            let toggledSetting = not settings.EmbedImages
            { settings with EmbedImages = toggledSetting }

        let toggleQuietMode settings =
            let toggledSetting = not settings.QuietMode
            { settings with QuietMode = toggledSetting }

        let updateAudioFormat settings (newFormat: string) =
            let updatedSettings = { settings with AudioFormats = newFormat.Split(',')}
            validate updatedSettings

        let updateAudioQuality settings newQuality =
            let updatedSettings = { settings with AudioQuality = newQuality}
            validate updatedSettings
