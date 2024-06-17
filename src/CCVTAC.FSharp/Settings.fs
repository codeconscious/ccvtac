namespace CCVTAC.FSharp

module Settings =
    open System.Text.Json.Serialization
    open System

    type FilePath = FilePath of string

    type RenamePattern = {
        [<JsonPropertyName("regex")>]          Regex : string
        [<JsonPropertyName("replacePattern")>] ReplaceWithPattern : string
        [<JsonPropertyName("description")>]    Description : string
    }

    type UserSettings = {
        [<JsonPropertyName("workingDirectory")>]             WorkingDirectory: string
        [<JsonPropertyName("moveToDirectory")>]              MoveToDirectory: string
        [<JsonPropertyName("historyFile")>]                  HistoryFile: string
        [<JsonPropertyName("historyDisplayCount")>]          HistoryDisplayCount: byte
        [<JsonPropertyName("splitChapters")>]                SplitChapters: bool
        [<JsonPropertyName("sleepSecondsBetweenDownloads")>] SleepSecondsBetweenDownloads: uint16
        [<JsonPropertyName("sleepSecondsBetweenBatches")>]   SleepSecondsBetweenBatches: uint16
        [<JsonPropertyName("verboseOutput")>]                VerboseOutput: bool
        [<JsonPropertyName("embedImages")>]                  EmbedImages: bool
        [<JsonPropertyName("doNotEmbedImageUploaders")>]     DoNotEmbedImageUploaders: string array
        [<JsonPropertyName("ignoreUploadYearUploaders")>]    IgnoreUploadYearUploaders: string array
        [<JsonPropertyName("renamePatterns")>]               RenamePatterns: RenamePattern array
    }

    [<CompiledName("Summarize")>]
    let summarize settings =
        let pluralize label count =
            if count = 1
            then $"{count} {label}"
            else $"{count} {label}s"

        [
            ("Split video chapters", if settings.SplitChapters then "ON" else "OFF")
            ("Verbose mode", if settings.VerboseOutput then "ON" else "OFF")
            ("Embed images", if settings.EmbedImages then "ON" else "OFF")
            ("Sleep between batches", settings.SleepSecondsBetweenBatches |> int |> pluralize  "second")
            ("Sleep between downloads", settings.SleepSecondsBetweenDownloads |> int |> pluralize "second")
            ("Ignore-upload-year channels", settings.IgnoreUploadYearUploaders.Length |> pluralize "channel")
            ("Do-not-embed-image channels", settings.DoNotEmbedImageUploaders.Length |> pluralize "channel")
            ("Rename patterns", settings.RenamePatterns.Length |> pluralize "pattern")
            ("Working directory", settings.WorkingDirectory)
            ("Move-to directory", settings.MoveToDirectory)
            ("History log file", settings.HistoryFile)
        ]

    module IO =
        open System.IO
        open System.Text.Json
        open System.Text.Unicode
        open System.Text.Encodings.Web

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

                match settings with
                | { WorkingDirectory = w } when isEmpty w ->
                    Error $"No working directory was specified."
                | { WorkingDirectory = w } when dirMissing w ->
                    Error $"Working directory \"{w}\" is missing."
                | { MoveToDirectory = m } when isEmpty m ->
                    Error $"No move-to directory was specified."
                | { MoveToDirectory = m } when dirMissing m ->
                    Error $"Move-to directory \"{m}\" is missing."
                | _ ->
                    Ok settings

            try
                path
                |> File.ReadAllText
                |> JsonSerializer.Deserialize<UserSettings>
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
                  VerboseOutput = true
                  EmbedImages = true
                  DoNotEmbedImageUploaders = [||]
                  IgnoreUploadYearUploaders = [||]
                  RenamePatterns = [||] }
            writeFile defaultSettings confirmedPath
