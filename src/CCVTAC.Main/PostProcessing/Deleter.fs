namespace CCVTAC.Main.PostProcessing

open CCVTAC.Main
open System.IO

module Deleter =
    let private getCollectionFiles
        (collectionMetadata: CollectionMetadata option)
        (workingDirectory: string)
        : Result<string array, string> =

        match collectionMetadata with
        | None -> Ok [||]
        | Some metadata ->
            try Ok (Directory.GetFiles(workingDirectory, $"*{metadata.Id}*"))
            with exn -> Error $"Error collecting filenames: {exn.Message}"

    let private deleteAll (fileNames: string array) (printer: Printer) : unit =
        fileNames
        |> Array.iter (fun fileName ->
            try
                File.Delete fileName
                printer.Debug $"• Deleted \"{fileName}\""
            with
            | ex -> printer.Error $"• Deletion error: {ex.Message}"
        )

    let run
        (taggingSetFileNames: string seq)
        (collectionMetadata: CollectionMetadata option)
        (workingDirectory: string)
        (printer: Printer)
        : unit =

        let collectionFileNames =
            match getCollectionFiles collectionMetadata workingDirectory with
            | Ok files ->
                printer.Debug $"""Found {String.fileLabelWithDescriptor "collection" files.Length}."""
                files
            | Error err ->
                printer.Warning err
                [||]

        let allFileNames = Seq.concat [taggingSetFileNames; collectionFileNames] |> Seq.toArray

        if Array.isEmpty allFileNames then
            printer.Warning "No files to delete were found."
        else
            printer.Debug $"""Deleting {String.fileLabelWithDescriptor "temporary" allFileNames.Length}..."""
            deleteAll allFileNames printer
            printer.Info "Deleted temporary files."
