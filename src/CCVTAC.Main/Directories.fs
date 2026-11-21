namespace CCVTAC.Main.IoUtilities

open Files
open CCVTAC.Main
open CCFSharpUtils
open CCFSharpUtils.Collections
open CCFSharpUtils.Text
open System.IO

module Directories =

    [<Literal>]
    let private allFilesSearchPattern = "*"

    [<Literal>]
    let maxFilesToShow = 10

    /// Counts the number of audio files in a directory.
    let audioFileCount dirName includedExtensions =
        DirectoryInfo(dirName).EnumerateFiles()
        |> Seq.filter (fun f -> List.containsIgnoreCase f.Extension includedExtensions)
        |> Seq.length

    /// Returns the filenames in a given directory, optionally ignoring specific filenames.
    let private getDirectoryFileNames dirName ignoreFilesOpt : Result<string array, string> =
        let ignoreFiles : string list =
            defaultArg ignoreFilesOpt []
            |> Seq.distinct
            |> Seq.toList

        ofTry (fun _ ->
            Directory.GetFiles(dirName, allFilesSearchPattern, EnumerationOptions())
            |> Array.filter (fun filePath -> not (ignoreFiles |> List.exists filePath.EndsWith)))

    let deleteAllFiles workingDirectory : Result<OutcomeCounts option, string> =
        let delete fileNames =
            if Seq.isEmpty fileNames then
                None
            else
                let successes, failures = ResizeArray<string>(), ResizeArray<string>()

                for fileName in fileNames do
                    try
                        File.Delete fileName
                        successes.Add $"• %s{fileName}"
                    with exn ->
                        failures.Add $"• Error deleting \"%s{fileName}\": %s{exn.Message}"

                Some { Successes = List.ofSeq successes
                       Failures  = List.ofSeq failures }

        getDirectoryFileNames workingDirectory None
        |> Result.map delete

    let printDeletionResults (printer: Printer) outcome : unit =
        match outcome with
        | None ->
            printer.Info "No files for deletion found."
        | Some counts ->
            printer.Info $"Deleted %s{countFiles counts.Successes.Length}."
            counts.Successes |> List.iter printer.Debug

            if List.isNotEmpty counts.Failures then
                printer.Error $"%s{countFiles counts.Failures.Length} could not be deleted:"
                counts.Failures |> List.iter printer.Error

    let warnIfAnyFiles showMax dirName : Result<unit, string> =
        match getDirectoryFileNames dirName None with
        | Error errMsg -> Error errMsg
        | Ok fileNames ->
            if Array.isEmpty fileNames then
                Ok ()
            else
                SB($"""Found {String.plur' "leftover temporary file" S fileNames.Length} in "{dirName}":{String.nl}""")
                    .AppendLine
                        (fileNames
                         |> Array.truncate showMax
                         |> Array.sort
                         |> Array.map (sprintf "• %s")
                         |> String.concatNL)
                |> fun sb ->
                    if fileNames.Length > showMax
                    then sb.AppendLine $"... plus {fileNames.Length - showMax} more."
                    else sb
                // |> _.AppendLine("This sometimes occurs due to the same video appearing twice in playlists.")
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
