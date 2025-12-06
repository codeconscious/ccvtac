namespace CCVTAC.Console.IoUtilities

open System.IO
open CCVTAC.Console

module Directories =

    type private ErrorList = string ResizeArray

    [<Literal>]
    let private allFilesSearchPattern = "*"

    /// Counts the number of audio files in a directory
    let audioFileCount (directory: string) =
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

        Directory.GetFiles(directoryName, allFilesSearchPattern, EnumerationOptions())
        |> Array.filter (fun filePath -> not (ignoreFiles |> Array.exists filePath.EndsWith))

    /// Deletes all files in the working directory
    let deleteAllFiles showMaxErrors workingDirectory : Result<uint,string> =
        let fileNames = getDirectoryFileNames workingDirectory None

        let successCount, errors =
            Array.fold
                (fun (s: uint, errs: ErrorList) fileName ->
                    try File.Delete fileName
                        (s + 1u, errs)
                    with ex -> errs.Add ex.Message; s, errs)
                (0u, ErrorList())
                fileNames

        if errors.Count = 0 then
            Ok successCount
        else
            SB($"While {successCount} files were deleted successfully, some files could not be deleted:{String.newLine}")
                .AppendLine
                    (fileNames
                     |> Array.truncate showMaxErrors
                     |> Array.map (sprintf "• %s")
                     |> String.concat String.newLine)
            |> fun sb ->
                if errors.Count > showMaxErrors
                then sb.AppendLine $"... plus {errors.Count - showMaxErrors} more."
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
        let fileNames = getDirectoryFileNames dirName None

        if Array.isEmpty fileNames then
            Ok ()
        else
            let fileLabel = NumberUtilities.pluralize "file" "files" fileNames.Length

            SB($"Unexpectedly found {fileNames.Length} {fileLabel} in working directory \"{dirName}\":{String.newLine}")
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

