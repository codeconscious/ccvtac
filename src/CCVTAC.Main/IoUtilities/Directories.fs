namespace CCVTAC.Main.IoUtilities

open CCVTAC.Main
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

    let deleteAllFiles workingDirectory
        : Result<ResultMessageCollection, string> =

        let delete fileNames =
            let successes, failures = ResizeArray<string>(), ResizeArray<string>()

            for fileName in fileNames do
                try
                    File.Delete fileName
                    successes.Add $"• Deleted \"%s{fileName}\""
                with exn ->
                    failures.Add $"• Error deleting \"%s{fileName}\": %s{exn.Message}"

            { Successes = successes |> Seq.toList |> List.rev
              Failures  = failures  |> Seq.toList |> List.rev }

        match getDirectoryFileNames workingDirectory None with
        | Error errMsg -> Error errMsg
        | Ok fileNames -> Ok (delete fileNames)

    /// Ask the user to confirm the deletion of files in the specified directory.
    let askToDeleteAllFiles dirName (printer: Printer) =
        if printer.AskToBool("Delete all temporary files?", "Yes", "No")
        then deleteAllFiles dirName
        else Error "Will not delete the files."

    let printDeletionResults (printer: Printer) (results: ResultMessageCollection) : unit =
        printer.Info $"Deleted %s{String.fileLabel results.Successes.Length}."
        results.Successes |> List.iter printer.Debug

        if List.isNotEmpty results.Failures then
            printer.Warning $"However, %s{String.fileLabel results.Failures.Length} could not be deleted:"
            results.Failures |> List.iter printer.Error

    let warnIfAnyFiles showMax dirName =
        match getDirectoryFileNames dirName None with
        | Error errMsg -> Error errMsg
        | Ok fileNames ->
            if Array.isEmpty fileNames then
                Ok ()
            else
                SB($"Unexpectedly found {String.fileLabel fileNames.Length} in working directory \"{dirName}\":{String.newLine}")
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

