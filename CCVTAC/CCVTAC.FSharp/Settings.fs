namespace CCVTAC.FSharp.Settings

// TODO: Make proper modules and ctors?
// TODO: Verify existance and validity?
type DirectoryName = DirectoryName of string
type FilePath = FilePath of string

// TODO: Add attributes for custom names (e.g., `[<field: DataMember(Name="workingDirectory")>]`)
type UserSettings = {
    WorkingDirectory : string
    MoveToDirectory : string
    HistoryFile : string // Needs to be HistoryFile
    HistoryDisplayCount : byte
    SplitChapters : bool
    SleepSecondsBetweenDownloads : uint16
    SleepSecondsBetweenBatches : uint16
    VerboseOutput: bool
    IgnoreUploadYearUploaders : string array
}

module IO =
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Unicode
    open System.Text.Encodings.Web

    [<CompiledName("Read")>]
    let read filePath =
        let (FilePath file) = filePath

        let verify (settings:UserSettings) =
            let isBlank str = String.IsNullOrWhiteSpace str
            let dirMissing str = not <| (Directory.Exists str)

            match settings with
            | { WorkingDirectory = w } when isBlank w
                -> Error $"No working directory was specified."
            | { MoveToDirectory = m } when isBlank m
                -> Error $"No move-to directory was specified."
            | { WorkingDirectory = w } when dirMissing w
                -> Error $"Working directory \"{w}\" does not exist."
            | { MoveToDirectory = m } when dirMissing m
                -> Error $"Move-to directory \"{m}\" does not exist."
            | _ -> Ok settings

        try
            file
            |> File.ReadAllText
            |> JsonSerializer.Deserialize<UserSettings>
            |> verify
        with
            | :? FileNotFoundException -> Error $"\"{file}\" was not found."
            | :? JsonException -> Error $"Could not parse JSON in \"{file}\" to settings."
            | e -> Error $"Settings unexpectedly could not be read from \"{file}\": {e.Message}"

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
            | :? FileNotFoundException -> Error $"\"{file}\" was not found and did not create it."
            | :? JsonException -> Error $"Could not parse user settings to JSON."
            | e -> Error $"Unexpectedly could not write settings to \"{file}\": {e.Message}"

    [<CompiledName("WriteDefaultFile")>]
    let writeDefaultFile filePath =
        let defaultSettings = {
            WorkingDirectory = String.Empty
            MoveToDirectory = String.Empty
            HistoryFile = String.Empty
            HistoryDisplayCount = 25uy // byte
            SplitChapters = true
            SleepSecondsBetweenDownloads = 10us // uint16
            SleepSecondsBetweenBatches = 20us // uint16
            VerboseOutput = true
            IgnoreUploadYearUploaders = [||]
        }
        writeFile defaultSettings filePath
