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
        |> Seq.filter (fun f -> List.caseInsensitiveContains f.Extension audioExtensions)
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
    let internal deleteAllFiles showMaxErrors workingDirectory =
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
        then deleteAllFiles 10 workingDirectory
        else Error "Will not delete the files."

    let internal warnIfAnyFiles showMax dirName =
        let fileNames = getDirectoryFileNames dirName None

        if Array.isEmpty fileNames then
            Ok ()
        else
            let fileLabel : int -> string = NumberUtilities.pluralize "file" "files"
            let report = StringBuilder()

            report.AppendLine $"Unexpectedly found {fileNames.Length} {fileLabel} in working directory \"{dirName}\":"
                |> ignore

            report.AppendLine
                (fileNames
                 |> Array.truncate showMax
                 |> Array.map (sprintf "• %s")
                 |> String.concat String.newLine) |> ignore

            if fileNames.Length > showMax then
                report.AppendLine($"... plus {fileNames.Length - showMax} more.") |> ignore

            report.AppendLine("This sometimes occurs due to the same video appearing twice in playlists.") |> ignore

            Error (report.ToString())

    let ensureDirectoryExists dirName : Result<DirectoryInfo, string> =
        try
            dirName
            |> Path.GetFullPath
            |> Directory.CreateDirectory
            |> Ok
        with exn ->
            Error $"Error accessing or creating directory \"%s{dirName}\": %s{exn.Message}"

