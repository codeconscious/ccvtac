namespace CCVTAC.Console.Settings

open System
open System.Text.Json.Serialization
open CCVTAC.Console
open Spectre.Console

module Settings =

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
        [<JsonPropertyName("title")>]     Title : TagDetectionPattern list
        [<JsonPropertyName("artist")>]    Artist : TagDetectionPattern list
        [<JsonPropertyName("album")>]     Album : TagDetectionPattern list
        [<JsonPropertyName("composer")>]  Composer : TagDetectionPattern list
        [<JsonPropertyName("year")>]      Year : TagDetectionPattern list
    }

    type UserSettings = {
        [<JsonPropertyName("workingDirectory")>]              WorkingDirectory: string
        [<JsonPropertyName("moveToDirectory")>]               MoveToDirectory: string
        [<JsonPropertyName("historyFile")>]                   HistoryFile: string
        [<JsonPropertyName("historyDisplayCount")>]           HistoryDisplayCount: int
        [<JsonPropertyName("audioFormats")>]                  AudioFormats: string list
        [<JsonPropertyName("audioQuality")>]                  AudioQuality: byte
        [<JsonPropertyName("splitChapters")>]                 SplitChapters: bool
        [<JsonPropertyName("sleepSecondsBetweenDownloads")>]  SleepSecondsBetweenDownloads: uint16
        [<JsonPropertyName("sleepSecondsBetweenURLs")>]       SleepSecondsBetweenURLs: uint16
        [<JsonPropertyName("quietMode")>]                     QuietMode: bool
        [<JsonPropertyName("embedImages")>]                   EmbedImages: bool
        [<JsonPropertyName("doNotEmbedImageUploaders")>]      DoNotEmbedImageUploaders: string list
        [<JsonPropertyName("ignoreUploadYearUploaders")>]     IgnoreUploadYearUploaders: string list
        [<JsonPropertyName("tagDetectionPatterns")>]          TagDetectionPatterns: TagDetectionPatterns
        [<JsonPropertyName("renamePatterns")>]                RenamePatterns: RenamePattern list
        [<JsonPropertyName("normalizationForm")>]             NormalizationForm : string
        [<JsonPropertyName("downloaderUpdateCommand")>]       DownloaderUpdateCommand : string
        [<JsonPropertyName("downloaderAdditionalOptions")>]   DownloaderAdditionalOptions : string option
    }

    let private defaultSettings =
        {
            WorkingDirectory = String.Empty
            MoveToDirectory = String.Empty
            HistoryFile = String.Empty
            HistoryDisplayCount = 25
            SplitChapters = true
            SleepSecondsBetweenDownloads = 10us
            SleepSecondsBetweenURLs = 15us
            AudioFormats = []
            AudioQuality = 0uy
            QuietMode = false
            EmbedImages = true
            DoNotEmbedImageUploaders = []
            IgnoreUploadYearUploaders = []
            TagDetectionPatterns = {
                Title = []
                Artist = []
                Album = []
                Composer = []
                Year = []
            }
            RenamePatterns = []
            NormalizationForm = "C" // Recommended for compatibility between Linux and macOS.
            DownloaderUpdateCommand = String.Empty
            DownloaderAdditionalOptions = None
        }

    let summarize settings : (string * string) list =
        let onOrOff = function
            | true -> "ON"
            | false -> "OFF"

        let simplePluralize label count =
            String.pluralize label $"{label}s" count
            |> sprintf "%d %s" count

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
            ("Audio quality (10 up to 0)", settings.AudioQuality |> sprintf "%d")
            ("Sleep between URLs", settings.SleepSecondsBetweenURLs |> int |> simplePluralize "second")
            ("Sleep between downloads", settings.SleepSecondsBetweenDownloads |> int |> simplePluralize "second")
            ("Ignore-upload-year channels", settings.IgnoreUploadYearUploaders.Length |> simplePluralize "channel")
            ("Do-not-embed-image channels", settings.DoNotEmbedImageUploaders.Length |> simplePluralize "channel")
            ("Tag-detection patterns", tagDetectionPatternCount settings.TagDetectionPatterns |> simplePluralize "pattern")
            ("Rename patterns", settings.RenamePatterns.Length |> simplePluralize "pattern")
        ]

    let printSummary settings (printer: Printer) headerOpt : unit =
        match headerOpt with
        | Some h when String.hasText h -> printer.Info h
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
            let dirMissing str = not (Directory.Exists str)

            // Source: https://github.com/yt-dlp/yt-dlp/?tab=readme-ov-file#post-processing-options
            let supportedAudioFormats = [ "best"; "aac"; "alac"; "flac"; "m4a"; "mp3"; "opus"; "vorbis"; "wav" ]
            let supportedNormalizationForms = [ "C"; "D"; "KC"; "KD" ]

            let validAudioFormat fmt = supportedAudioFormats |> List.contains fmt

            match settings with
            | { WorkingDirectory = dir } when String.hasNoText dir ->
                Error "No working directory was specified in the settings."
            | { WorkingDirectory = dir } when dirMissing dir ->
                Error $"Working directory \"{dir}\" is missing."
            | { MoveToDirectory = dir } when String.hasNoText dir ->
                Error "No move-to directory was specified in the settings."
            | { MoveToDirectory = dir } when dirMissing dir ->
                Error $"Move-to directory \"{dir}\" is missing."
            | { AudioQuality = q } when q < 0uy || q > 10uy ->
                Error "Audio quality must be in the range 10 (lowest) and 0 (highest)."
            | { NormalizationForm = nf } when not(supportedNormalizationForms |> List.contains (nf.ToUpperInvariant())) ->
                let okFormats = String.Join(", ", supportedNormalizationForms)
                Error $"\"{nf}\" is an invalid normalization form. Use one of the following: {okFormats}."
            | { AudioFormats = fmt } when not (fmt |> List.forall validAudioFormat) ->
                let formats = String.Join(", ", fmt)
                let approved = supportedAudioFormats |> String.concat ", "
                Error $"Audio formats (\"%s{formats}\") include an unsupported audio format.{String.newLine}Only the following supported formats: {approved}."
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
            try
                match JsonSerializer.Deserialize<'a>(json, options) with
                | null -> Error "Could not deserialize the settings JSON"
                | s -> Ok s
            with e -> Error e.Message

        let read (fileInfo: FileInfo) : Result<UserSettings, string> =
            try
                fileInfo.FullName
                |> File.ReadAllText
                |> deserialize<UserSettings>
                |> Result.bind validate
            with
                | :? FileNotFoundException -> Error $"File \"{fileInfo.FullName}\" was not found."
                | :? JsonException as e -> Error $"Parse error in \"{fileInfo.FullName}\": {e.Message}"
                | e -> Error $"Unexpected error reading from \"{fileInfo.FullName}\": {e.Message}"

        let private writeFile (filePath: FileInfo) settings : Result<string, string> =
            let unicodeEncoder = JavaScriptEncoder.Create UnicodeRanges.All
            let writeIndented = true
            let options = JsonSerializerOptions(WriteIndented = writeIndented, Encoder = unicodeEncoder)

            try
                let json = JsonSerializer.Serialize(settings, options)
                File.WriteAllText(filePath.FullName, json)
                Ok $"A new settings file was saved to \"{filePath.FullName}\". Please populate it with your desired settings."
            with
                | :? FileNotFoundException -> Error $"File \"{filePath.FullName}\" was not found."
                | :? JsonException -> Error "Failure parsing user settings to JSON."
                | e -> Error $"Unexpected error writing \"{filePath.FullName}\": {e.Message}"

        let writeDefaultFile (fileInfo: FileInfo) : Result<string, string> =
            writeFile fileInfo defaultSettings

    module LiveUpdating =
        open Validation

        let toggleSplitChapters settings =
            { settings with SplitChapters = not settings.SplitChapters }

        let toggleEmbedImages settings =
            { settings with EmbedImages = not settings.EmbedImages }

        let toggleQuietMode settings =
            { settings with QuietMode = not settings.QuietMode }

        let updateAudioFormat settings (newFormat: string) =
            let updatedSettings = { settings with AudioFormats = newFormat.Split ',' |> List.ofArray }
            validate updatedSettings

        let updateAudioQuality settings newQuality =
            let updatedSettings = { settings with AudioQuality = newQuality }
            validate updatedSettings
