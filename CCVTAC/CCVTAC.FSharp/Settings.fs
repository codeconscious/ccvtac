namespace CCVTAC.FSharp.Settings

// TODO: Make proper modules and ctors?
// TODO: Verify existance and validity?
type DirectoryName = DirectoryName of string
type FilePath = FilePath of string

// TODO: Add validation and create `ValidatedUserSettings`?
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

    [<CompiledName("Read")>]
    let read filePath =
        let (FilePath file) = filePath

        try
            Ok (file |> File.ReadAllText |> JsonSerializer.Deserialize<UserSettings>)
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
            PauseBeforePostProcessing = false // Debugging only; can it be ignored? Maybe best to just remove at this point...
            IgnoreUploadYearUploaders = [||]
        }
        writeFile defaultSettings filePath

    let checkValidity settings =
        raise <| NotImplementedException()
