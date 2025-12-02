namespace CCVTAC.Console.IoUtilities

open System
open System.IO
open System.Text
open CCVTAC.Console

module Directories =

    [<Literal>]
    let private allFilesSearchPattern = "*"

    let private enumerationOptions = EnumerationOptions()

    /// Counts the number of audio files in a directory
    let internal audioFileCount (directory: string) =
        DirectoryInfo(directory).EnumerateFiles()
        |> Seq.filter (fun f -> Seq.caseInsensitiveContains f.Extension AudioExtensions)
        |> Seq.length

    /// Returns the filenames in a given directory, optionally ignoring specific filenames
    let private getDirectoryFileNames
        (directoryName: string)
        (customIgnoreFiles: string seq option) =

        let ignoreFiles =
            customIgnoreFiles
            |> Option.defaultValue Seq.empty
            |> Seq.distinct
            |> Seq.toArray

        Directory.GetFiles(directoryName, allFilesSearchPattern, enumerationOptions)
        |> Array.filter (fun filePath -> not (ignoreFiles |> Array.exists filePath.EndsWith))

    /// Deletes all files in the working directory
    let internal deleteAllFiles (workingDirectory: string) (showMaxErrors: int) =
        let fileNames = getDirectoryFileNames workingDirectory None

        let mutable successCount = 0
        let errors = ResizeArray<string>() // TODO: Use an F# list.

        fileNames |> Array.iter (fun fileName ->
            try
                File.Delete fileName
                successCount <- successCount + 1
            with
            | ex -> errors.Add ex.Message
        )

        if errors.Count = 0 then
            Ok successCount
        else
            let output = StringBuilder(
                $"While {successCount} files were deleted successfully, some files could not be deleted:"
            )

            errors
            |> Seq.truncate showMaxErrors
            |> Seq.iter (fun error ->
                output.AppendLine($"• {error}") |> ignore
            )

            if errors.Count > showMaxErrors then
                output.AppendLine($"... plus {errors.Count - showMaxErrors} more.") |> ignore

            Error (output.ToString())

    /// Asks user if they want to delete all files
    let internal askToDeleteAllFiles (workingDirectory: string) (printer: Printer) =
        if printer.AskToBool("Delete all temporary files?", "Yes", "No")
        then deleteAllFiles workingDirectory 10
        else Error "Will not delete the files."

    let internal warnIfAnyFiles (directory: string) (showMax: int) =
        let fileNames = getDirectoryFileNames directory None

        if fileNames.Length = 0 then
            Ok()
        else
            let fileLabel = if fileNames.Length = 1 then "file" else "files"
            let report = StringBuilder()

            report.AppendLine(
                $"Unexpectedly found {fileNames.Length} {fileLabel} in working directory \"{directory}\":"
            ) |> ignore

            fileNames
            |> Array.truncate showMax
            |> Array.iter (fun fileName ->
                report.AppendLine($"• {fileName}") |> ignore
            )

            if fileNames.Length > showMax then
                report.AppendLine($"... plus {fileNames.Length - showMax} more.") |> ignore

            report.AppendLine("This generally occurs due to the same video appearing twice in playlists.") |> ignore

            Error (report.ToString())
