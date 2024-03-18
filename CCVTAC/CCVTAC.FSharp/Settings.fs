namespace CCVTAC.FSharp.Settings

open System.Text.Json.Serialization

// TODO: Make proper modules and ctors?
type DirectoryName = DirectoryName of string
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

module IO =
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Unicode
    open System.Text.Encodings.Web

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
            Ok (File.WriteAllText(file, json))
        with
            | :? FileNotFoundException -> Error $"\"{file}\" was not found."
            | :? JsonException -> Error $"Failure parsing user settings to JSON."
            | e -> Error $"Failure writing settings to \"{file}\": {e.Message}"

    [<CompiledName("WriteDefaultFile")>]
    let writeDefaultFile filePath =
        let defaultSettings =
            { WorkingDirectory = String.Empty
              MoveToDirectory = String.Empty
              HistoryFile = String.Empty
              HistoryDisplayCount = 25uy // byte
              SplitChapters = true
              SleepSecondsBetweenDownloads = 10us // uint16
              SleepSecondsBetweenBatches = 20us // uint16
              VerboseOutput = true
              IgnoreUploadYearUploaders = [||] }
        writeFile defaultSettings filePath
