namespace CCVTAC.Main.PostProcessing

open CCVTAC.Main
open CCFSharpUtils.Text
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

    let private deleteAll  (printer: Printer) (fileNames: string array) : unit =
        fileNames
        |> Array.iter (fun fileName ->
            try
                File.Delete fileName
                printer.Debug $"• Deleted \"{fileName}\""
            with
            | ex -> printer.Error $"• Deletion error: {ex.Message}"
        )

    let run
        (printer: Printer)
        (taggingSetFileNames: string seq)
        (collectionMetadata: CollectionMetadata option)
        (workingDirectory: string)
        : unit =

        let collectionFileNames =
            match getCollectionFiles collectionMetadata workingDirectory with
            | Ok files ->
                printer.Debug $"""Found {String.fileLabelWithDesc "collection" files.Length}."""
                files
            | Error err ->
                printer.Warning err
                [||]

        let allFileNames = Seq.concat [taggingSetFileNames; collectionFileNames] |> Seq.toArray

        if Array.isEmpty allFileNames then
            printer.Warning "No files to delete were found."
        else
            printer.Debug $"""Deleting {String.fileLabelWithDesc "temporary" allFileNames.Length}..."""
            deleteAll printer allFileNames
            printer.Info "Deleted temporary files."
