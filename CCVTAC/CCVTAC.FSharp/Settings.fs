namespace CCVTAC.FSharp

module Settings =
    open System.Text.Json.Serialization

    type FilePath = FilePath of string

    type UserSettings = {
        [<JsonPropertyName("workingDirectory")>]             WorkingDirectory: string
        [<JsonPropertyName("moveToDirectory")>]              MoveToDirectory: string
        [<JsonPropertyName("historyFile")>]                  HistoryFile: string
        [<JsonPropertyName("historyDisplayCount")>]          HistoryDisplayCount: byte
        [<JsonPropertyName("splitChapters")>]                SplitChapters: bool
        [<JsonPropertyName("sleepSecondsBetweenDownloads")>] SleepSecondsBetweenDownloads: uint16
        [<JsonPropertyName("sleepSecondsBetweenBatches")>]   SleepSecondsBetweenBatches: uint16
        [<JsonPropertyName("verboseOutput")>]                VerboseOutput: bool
        [<JsonPropertyName("ignoreUploadYearUploaders")>]    IgnoreUploadYearUploaders: string array
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
            ("Sleep between batches", settings.SleepSecondsBetweenBatches |> int |> pluralize  "second")
            ("Sleep between downloads", settings.SleepSecondsBetweenDownloads |> int |> pluralize "second")
            ("Ignore-upload-year channels", settings.IgnoreUploadYearUploaders.Length |> pluralize "channel")
            ("Working directory", settings.WorkingDirectory)
            ("Move-to directory", settings.MoveToDirectory)
            ("History log file", settings.HistoryFile)
        ]

    module IO =
        open System
        open System.IO
        open System.Text.Json
        open System.Text.Unicode
        open System.Text.Encodings.Web

        [<CompiledName("FileExists")>]
        let fileExists filePath =
            let (FilePath file) = filePath
            match file |> File.Exists with
            | true -> Ok()
            | false -> Error($"The file \"path\" does not exist.")

        [<CompiledName("Read")>]
        let read filePath =
            let (FilePath path) = filePath

            let verify (settings:UserSettings) =
                let isEmpty str = String.IsNullOrWhiteSpace str
                let dirMissing str = not <| (Directory.Exists str)

                match settings with
                | { WorkingDirectory = w } when isEmpty w -> Error $"No working directory was specified."
                | { WorkingDirectory = w } when dirMissing w -> Error $"Working directory \"{w}\" is missing."
                | { MoveToDirectory = m } when isEmpty m -> Error $"No move-to directory was specified."
                | { MoveToDirectory = m } when dirMissing m -> Error $"Move-to directory \"{m}\" is missing."
                | _ -> Ok settings

            try
                path
                |> File.ReadAllText
                |> JsonSerializer.Deserialize<UserSettings>
                |> verify
            with
                | :? FileNotFoundException -> Error $"\"{path}\" was not found."
                | :? JsonException -> Error $"Could not parse JSON in \"{path}\" to settings."
                | e -> Error $"Settings unexpectedly could not be read from \"{path}\": {e.Message}"

        [<CompiledName("WriteFile")>]
        let private writeFile settings filePath =
            let unicodeEncoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            let writeIndented = true
            let options = JsonSerializerOptions(WriteIndented = writeIndented, Encoder = unicodeEncoder)
            let (FilePath file) = filePath

            try
                let json = JsonSerializer.Serialize(settings, options)
                File.WriteAllText(file, json)
                Ok $"A new default settings file was created at \"{file}\".\nPlease populate it with your desired settings."
            with
                | :? FileNotFoundException -> Error $"\"{file}\" was not found."
                | :? JsonException -> Error $"Failure parsing user settings to JSON."
                | e -> Error $"Failure writing settings to \"{file}\": {e.Message}"

        [<CompiledName("WriteDefaultFile")>]
        let writeDefaultFile (filePath: FilePath option) =
            let safePath =
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
                  IgnoreUploadYearUploaders = [||] }
            writeFile defaultSettings safePath
