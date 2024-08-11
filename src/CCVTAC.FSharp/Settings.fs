namespace CCVTAC.FSharp

module Settings =
    open System.Text.Json.Serialization
    open System

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

        let pluralize label count =
            if count = 1
            then $"{count} {label}"
            else $"{count} {label}s"

        let tagDetectionPatternCount (patterns:TagDetectionPatterns) =
            patterns.Title.Length +
            patterns.Artist.Length +
            patterns.Album.Length +
            patterns.Composer.Length +
            patterns.Year.Length

        let summarizeAudioFormat (format:string) =
            match format with
            | "" -> "None specified (Will use default)"
            | _ -> format

        [
            ("Working directory",
             settings.WorkingDirectory)
            ("Move-to directory",
             settings.MoveToDirectory)
            ("History log file",
             settings.HistoryFile)
            ("Split video chapters",
             onOrOff settings.SplitChapters)
            ("Embed images",
             onOrOff settings.EmbedImages)
            ("Quiet mode",
             onOrOff settings.QuietMode)
            ("Audio format",
             summarizeAudioFormat settings.AudioFormat)
            ("Audio quality (10 up to 0)",
             settings.AudioQuality |> sprintf "%B")
            ("Sleep between batches (URLs)",
             settings.SleepSecondsBetweenBatches |> int |> pluralize "second")
            ("Sleep between downloads",
             settings.SleepSecondsBetweenDownloads |> int |> pluralize "second")
            ("Ignore-upload-year channels",
             settings.IgnoreUploadYearUploaders.Length |> pluralize "channel")
            ("Do-not-embed-image channels",
             settings.DoNotEmbedImageUploaders.Length |> pluralize "channel")
            ("Tag-detection patterns",
             tagDetectionPatternCount settings.TagDetectionPatterns |> pluralize "pattern")
            ("Rename patterns",
             settings.RenamePatterns.Length |> pluralize "pattern")
        ]

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

    module IO =
        open System.IO
        open System.Text.Json
        open System.Text.Unicode
        open System.Text.Encodings.Web

        let deserialize<'a> (json:string) =
            let options = new JsonSerializerOptions()
            options.AllowTrailingCommas <- true
            options.ReadCommentHandling <- JsonCommentHandling.Skip

            JsonSerializer.Deserialize<'a>(json, options)

        [<CompiledName("FileExists")>]
        let fileExists filePath =
            let (FilePath file) = filePath
            match file |> File.Exists with
            | true -> Ok()
            | false -> Error $"The file \"path\" does not exist."

        [<CompiledName("Read")>]
        let read filePath =
            let (FilePath path) = filePath

            let verify settings =
                let isEmpty str = str |> String.IsNullOrWhiteSpace
                let dirMissing str = not <| (Directory.Exists str)

                // Source: https://github.com/yt-dlp/yt-dlp/?tab=readme-ov-file#post-processing-options
                let supportedAudioFormats = [|"aac"; "alac"; "flac"; "m4a"; "mp3"; "opus"; "vorbis"; "wav"|]

                let validAudioFormat (format: string) =
                    match format with
                    | fmt when fmt = "default" -> true
                    | fmt when supportedAudioFormats |> Array.contains fmt -> true
                    | _ -> false

                match settings with
                | { WorkingDirectory = w } when isEmpty w ->
                    Error $"No working directory was specified."
                | { WorkingDirectory = w } when dirMissing w ->
                    Error $"Working directory \"{w}\" is missing."
                | { MoveToDirectory = m } when isEmpty m ->
                    Error $"No move-to directory was specified."
                | { MoveToDirectory = m } when dirMissing m ->
                    Error $"Move-to directory \"{m}\" is missing."
                | { AudioQuality = q } when q > 10uy ->
                    Error $"Audio quality must be between 10 (lowest) and 0 (highest)."
                | { AudioFormat = af } when not (validAudioFormat af) ->
                    let approved = supportedAudioFormats |> String.concat ", "
                    Error $"\"{af}\" is not a valid audio format. Use \"default\" or one of the following: {approved}."
                | _ ->
                    Ok settings

            try
                path
                |> File.ReadAllText
                |> deserialize<UserSettings>
                |> verify
            with
                | :? FileNotFoundException -> Error $"\"{path}\" was not found."
                | :? JsonException as e -> Error $"Could not parse settings file \"{path}\": {e.Message}"
                | e -> Error $"Settings unexpectedly could not be read from \"{path}\": {e.Message}"

        [<CompiledName("WriteFile")>]
        let private writeFile settings filePath =
            let unicodeEncoder = UnicodeRanges.All |> JavaScriptEncoder.Create
            let writeIndented = true
            let options = JsonSerializerOptions(WriteIndented = writeIndented, Encoder = unicodeEncoder)
            let (FilePath file) = filePath

            try
                let json = JsonSerializer.Serialize(settings, options)
                (file, json) |> File.WriteAllText
                Ok $"A new default settings file was created at \"{file}\".\nPlease populate it with your desired settings."
            with
                | :? FileNotFoundException -> Error $"\"{file}\" was not found."
                | :? JsonException -> Error $"Failure parsing user settings to JSON."
                | e -> Error $"Failure writing settings to \"{file}\": {e.Message}"

        [<CompiledName("WriteDefaultFile")>]
        let writeDefaultFile (filePath: FilePath option) =
            let confirmedPath =
                let defaultFileName = "settings.json"
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
