namespace CCVTAC.FSharp

module Settings =
    open System.Text.Json.Serialization
    open System

    let newLine = Environment.NewLine

    type FilePath = FilePath of string

    type RenamePattern = {
        [<JsonPropertyName("regex")>]           Regex : string
        [<JsonPropertyName("replacePattern")>]  ReplaceWithPattern : string
        [<JsonPropertyName("summary")>]         Summary : string
    }

    type TagDetectionPattern = {
        [<JsonPropertyName("regex")>]        Regex : string
        [<JsonPropertyName("matchGroup")>]   MatchGroup : byte
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
        [<JsonPropertyName("audioFormat")>]                   AudioFormat: string
        [<JsonPropertyName("audioQuality")>]                  AudioQuality: byte
        [<JsonPropertyName("splitChapters")>]                 SplitChapters: bool
        [<JsonPropertyName("sleepSecondsBetweenDownloads")>]  SleepSecondsBetweenDownloads: uint16
        [<JsonPropertyName("sleepSecondsBetweenBatches")>]    SleepSecondsBetweenBatches: uint16
        [<JsonPropertyName("quietMode")>]                     QuietMode: bool
        [<JsonPropertyName("embedImages")>]                   EmbedImages: bool
        [<JsonPropertyName("doNotEmbedImageUploaders")>]      DoNotEmbedImageUploaders: string array
        [<JsonPropertyName("ignoreUploadYearUploaders")>]     IgnoreUploadYearUploaders: string array
        [<JsonPropertyName("tagDetectionPatterns")>]          TagDetectionPatterns: TagDetectionPatterns
        [<JsonPropertyName("renamePatterns")>]                RenamePatterns: RenamePattern array
    }

    [<CompiledName("Summarize")>]
    let summarize settings =
        let onOrOff b =
            if b = true then "ON" else "OFF"

        let pluralize (label: string) count =
            if count = 1
            then $"{count} {label}"
            else $"{count} {label}s"

        let tagDetectionPatternCount (patterns:TagDetectionPatterns) =
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
            ("Audio format", settings.AudioFormat)
            ("Audio quality (10 up to 0)", settings.AudioQuality |> sprintf "%B")
            ("Sleep between batches", settings.SleepSecondsBetweenBatches |> int |> pluralize "second")
            ("Sleep between downloads", settings.SleepSecondsBetweenDownloads |> int |> pluralize "second")
            ("Ignore-upload-year channels", settings.IgnoreUploadYearUploaders.Length |> pluralize "channel")
            ("Do-not-embed-image channels", settings.DoNotEmbedImageUploaders.Length |> pluralize "channel")
            ("Tag-detection patterns", tagDetectionPatternCount settings.TagDetectionPatterns |> pluralize "pattern")
            ("Rename patterns", settings.RenamePatterns.Length |> pluralize "pattern")
        ]

    module Validation =
        open System.IO

        let validate settings =
            let isEmpty str = str |> String.IsNullOrWhiteSpace
            let dirMissing str = not <| (Directory.Exists str)

            // Source: https://github.com/yt-dlp/yt-dlp/?tab=readme-ov-file#post-processing-options
            let supportedAudioFormats = [|"best"; "aac"; "alac"; "flac"; "m4a"; "mp3"; "opus"; "vorbis"; "wav"|]

            let validAudioFormat fmt =
                supportedAudioFormats |> Array.contains fmt

            match settings with
            | { WorkingDirectory = d } when d |> isEmpty ->
                Error $"No working directory was specified."
            | { WorkingDirectory = d } when d |> dirMissing ->
                Error $"Working directory \"{d}\" is missing."
            | { MoveToDirectory = d } when d |> isEmpty ->
                Error $"No move-to directory was specified."
            | { MoveToDirectory = d } when d |> dirMissing ->
                Error $"Move-to directory \"{d}\" is missing."
            | { AudioQuality = q } when q > 10uy ->
                Error $"Audio quality must be in the range 10 (lowest) and 0 (highest)."
            | { AudioFormat = fmt } when not (fmt |> validAudioFormat) ->
                let approved = supportedAudioFormats |> String.concat ", "
                Error $"\"{fmt}\" is an invalid audio format. Use \"default\" or one of the following: {approved}."
            | _ ->
                Ok settings

    module IO =
        open System.IO
        open System.Text.Json
        open System.Text.Unicode
        open System.Text.Encodings.Web
        open Validation

        let deserialize<'a> (json:string) =
            let options = new JsonSerializerOptions()
            options.AllowTrailingCommas <- true
            options.ReadCommentHandling <- JsonCommentHandling.Skip

            JsonSerializer.Deserialize<'a>(json, options)

        [<CompiledName("FileExists")>]
        let fileExists (FilePath file) =
            match file |> File.Exists with
            | true -> Ok()
            | false -> Error $"The file \"path\" does not exist."

        [<CompiledName("Read")>]
        let read (FilePath path) =
            try
                path
                |> File.ReadAllText
                |> deserialize<UserSettings>
                |> validate
            with
                | :? FileNotFoundException -> Error $"File \"{path}\" was not found."
                | :? JsonException as e -> Error $"Parse error in \"{path}\": {e.Message}"
                | e -> Error $"Unexpected error reading from \"{path}\": {e.Message}"

        [<CompiledName("WriteFile")>]
        let private writeFile settings (FilePath file) =
            let unicodeEncoder = UnicodeRanges.All |> JavaScriptEncoder.Create
            let writeIndented = true
            let options = JsonSerializerOptions(WriteIndented = writeIndented, Encoder = unicodeEncoder)

            try
                let json = JsonSerializer.Serialize(settings, options)
                (file, json) |> File.WriteAllText
                Ok $"A new default settings file was created at \"{file}\".{newLine}Please populate it with your desired settings."
            with
                | :? FileNotFoundException -> Error $"\"{file}\" was not found."
                | :? JsonException -> Error $"Failure parsing user settings to JSON."
                | e -> Error $"Failure writing settings to \"{file}\": {e.Message}"

        [<CompiledName("WriteDefaultFile")>]
        let writeDefaultFile (filePath: FilePath option) defaultFileName =
            let confirmedPath =
                match filePath with
                | Some p -> p
                | None -> FilePath <| Path.Combine(AppContext.BaseDirectory, defaultFileName);
            let defaultSettings =
                { WorkingDirectory = String.Empty
                  MoveToDirectory = String.Empty
                  HistoryFile = String.Empty
                  HistoryDisplayCount = 25uy // byte
                  SplitChapters = true
                  SleepSecondsBetweenDownloads = 10us
                  SleepSecondsBetweenBatches = 20us
                  AudioFormat = String.Empty
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
                  RenamePatterns = [||] }
            writeFile defaultSettings confirmedPath

    module LiveUpdating =
        open Validation

        [<CompiledName("ToggleSplitChapters")>]
        let toggleSplitChapters settings =
            let toggledSetting = not <| settings.SplitChapters
            { settings with SplitChapters = toggledSetting }

        [<CompiledName("ToggleEmbedImages")>]
        let toggleEmbedImages settings =
            let toggledSetting = not <| settings.EmbedImages
            { settings with EmbedImages = toggledSetting }

        [<CompiledName("ToggleQuietMode")>]
        let toggleQuietMode settings =
            let toggledSetting = not <| settings.QuietMode
            { settings with QuietMode = toggledSetting }

        [<CompiledName("UpdateAudioFormat")>]
        let updateAudioFormat settings newFormat =
            let updatedSettings = { settings with AudioFormat = newFormat}
            validate updatedSettings

        [<CompiledName("UpdateAudioQuality")>]
        let updateAudioQuality settings newQuality =
            let updatedSettings = { settings with AudioQuality = newQuality}
            validate updatedSettings
