namespace CCVTAC.FSharp.Settings

// TODO: Make proper modules and ctors?
// TODO: Verify existance and validity?
type DirectoryName = DirectoryName of string
type FilePath = FilePath of string

// type UserSettings = {
//     WorkingDirectory : DirectoryName
//     MoveToDirectory : DirectoryName
//     HistoryFile : FilePath
//     HistoryDisplayCount : byte
//     SplitChapters : bool
//     SleepSecondsBetweenDownloads : uint16
//     SleepSecondsBetweenBatches : uint16
//     VerboseOutput: bool
//     PauseBeforePostProcessing : bool // Debugging use
// }
// TODO: Add validation and create `ValidatedUserSettings`?
// TODO: Add attributes for custom names (e.g., `[<field: DataMember(Name="workingDirectory")>]`)
type UserSettings = {
    WorkingDirectory : string
    MoveToDirectory : string
    HistoryFilePath : string // Needs to be HistoryFile
    HistoryDisplayCount : byte
    SplitChapters : bool
    SleepSecondsBetweenDownloads : uint16
    SleepSecondsBetweenBatches : uint16
    VerboseOutput: bool
    PauseBeforePostProcessing : bool // Debugging use
    IgnoreUploadYearUploaders : string array
}

module IO =
    open System
    open System.IO
    open System.Text.Json
    open System.Text.Unicode
    open System.Text.Encodings.Web

    // let tryIo f param =
    //     try Ok(f param)
    //     with ex -> Error(ex.Message)
    // let tryIo2 f param =
    //     try Ok(f param)
    //     with //ex -> Error(ex.Message)
    //         | :? FileNotFoundException -> Error($"\"{file}\" was not found.")
    //         | :? JsonException -> Error($"Could not parse JSON in \"{file}\" to settings.")
    //         | e -> Error $"Settings unexpectedly could not be read from \"{file}\": {e.Message}"

    let readSettings filePath =
        let (FilePath file) = filePath

        try
            Ok (file |> File.ReadAllText |> JsonSerializer.Deserialize<UserSettings>)
        with
            | :? FileNotFoundException -> Error($"\"{file}\" was not found.")
            | :? JsonException -> Error($"Could not parse JSON in \"{file}\" to settings.")
            | e -> Error $"Settings unexpectedly could not be read from \"{file}\": {e.Message}"

    let writeSettings settings filePath =
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

    let checkValidity settings =
        raise <| NotImplementedException()
