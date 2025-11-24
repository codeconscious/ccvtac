namespace CCVTAC.Console.PostProcessing

open System
open System.IO
open CCVTAC.Console

module Deleter =
    /// Retrieves collection files based on collection metadata
    let private getCollectionFiles
        (collectionMetadata: CollectionMetadata option)
        (workingDirectory: string)
        : Result<string[], string> =

        match collectionMetadata with
        | None -> Ok [||]
        | Some metadata ->
            try
                let files =
                    Directory.GetFiles(workingDirectory, $"*{metadata.Id}*")

                Ok files
            with
            | ex -> Error $"Error collecting filenames: {ex.Message}"

    /// Deletes all specified files
    let private deleteAll
        (fileNames: string[])
        (printer: Printer)
        : unit =

        fileNames
        |> Array.iter (fun fileName ->
            try
                File.Delete(fileName)
                printer.Debug($"• Deleted \"{fileName}\"")
            with
            | ex -> printer.Error($"• Deletion error: {ex.Message}")
        )

    /// Runs the deletion process for temporary files
    let internal run
        (taggingSetFileNames: string seq)
        (collectionMetadata: CollectionMetadata option)
        (workingDirectory: string)
        (printer: Printer)
        : unit =

        // Get collection files
        let collectionFileNames =
            match getCollectionFiles collectionMetadata workingDirectory with
            | Ok files ->
                printer.Debug($"Found {files.Length} collection files.")
                files
            | Error err ->
                printer.Warning(err)
                [||]

        // Combine all file names
        let allFileNames =
            Seq.concat [taggingSetFileNames; collectionFileNames]
            |> Seq.toArray

        // Check if any files to delete
        if allFileNames.Length = 0 then
            printer.Warning("No files to delete were found.")
        else
            printer.Debug($"Deleting {allFileNames.Length} temporary files...")
            deleteAll allFileNames printer
            printer.Info("Deleted temporary files.")
