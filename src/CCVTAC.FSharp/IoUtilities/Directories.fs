namespace CCVTAC.Console.IoUtilities

open CCVTAC.Console
open System.IO

module Directories =

    [<Literal>]
    let private allFilesSearchPattern = "*"

    /// Counts the number of audio files in a directory.
    let audioFileCount (directory: string) (includedExtensions: string list) =
        DirectoryInfo(directory).EnumerateFiles()
        |> Seq.filter (fun f -> List.caseInsensitiveContains f.Extension includedExtensions)
        |> Seq.length

    /// Returns the filenames in a given directory, optionally ignoring specific filenames.
    let private getDirectoryFileNames
        (directoryName: string)
        (customIgnoreFiles: string seq option)
        : Result<string array, string> =

        let ignoreFiles =
            customIgnoreFiles
            |> Option.defaultValue Seq.empty
            |> Seq.distinct
            |> Seq.toArray

        ofTry (fun _ ->
            Directory.GetFiles(directoryName, allFilesSearchPattern, EnumerationOptions())
            |> Array.filter (fun filePath -> not (ignoreFiles |> Array.exists filePath.EndsWith)))

    /// Empties a specified directory and reports the count of deleted files.
    let deleteAllFiles showMaxErrors workingDirectory : Result<uint,string> =
        let delete fileNames =
            Array.fold
                (fun (successCount: uint, errors: string list) fileName ->
                    try
                        File.Delete fileName
                        (successCount + 1u, errors)
                    with exn -> (successCount, errors @ [exn.Message]))
                (0u, [])
                fileNames

        match getDirectoryFileNames workingDirectory None with
        | Error errMsg -> Error errMsg
        | Ok fileNames ->
            match delete fileNames with
            | successCount, [] ->
                Ok successCount
            | successCount, errors ->
                SB($"{String.fileLabel None successCount} deleted successfully, but some files could not be deleted:{String.newLine}")
                    .AppendLine(fileNames
                                |> Array.truncate showMaxErrors
                                |> Array.map (sprintf "• %s")
                                |> String.concat String.newLine)
                |> fun sb ->
                    if errors.Length > showMaxErrors
                    then sb.AppendLine $"... plus {errors.Length - showMaxErrors} more."
                    else sb
                |> _.ToString()
                |> Error

    /// Ask the user to confirm the deletion of files in the specified directory.
    let askToDeleteAllFiles dirName (printer: Printer) =
        if printer.AskToBool("Delete all temporary files?", "Yes", "No")
        then deleteAllFiles 10 dirName
        else Error "Will not delete the files."

    /// Warn the user if there are any files in the specified directory.
    let warnIfAnyFiles showMax dirName =
        match getDirectoryFileNames dirName None with
        | Error errMsg -> Error errMsg
        | Ok fileNames ->
            if Array.isEmpty fileNames then
                Ok ()
            else
                SB($"Unexpectedly found {String.fileLabel None fileNames.Length} in working directory \"{dirName}\":{String.newLine}")
                    .AppendLine
                        (fileNames
                         |> Array.truncate showMax
                         |> Array.map (sprintf "• %s")
                         |> String.concat String.newLine)
                |> fun sb ->
                    if fileNames.Length > showMax
                    then sb.AppendLine $"... plus {fileNames.Length - showMax} more."
                    else sb
                |> _.AppendLine("This sometimes occurs due to the same video appearing twice in playlists.")
                |> _.ToString()
                |> Error

    /// Ensures the specified directory exists, including creation of it if necessary.
    let ensureDirectoryExists dirName : Result<DirectoryInfo, string> =
        try
            dirName
            |> Path.GetFullPath
            |> Directory.CreateDirectory
            |> Ok
        with exn ->
            Error $"Error accessing or creating directory \"%s{dirName}\": %s{exn.Message}"

