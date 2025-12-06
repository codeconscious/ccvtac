namespace CCVTAC.Console.PostProcessing

open System.IO
open System.Linq
open System.Text.Json
open System.Text.RegularExpressions
open CCVTAC.Console
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.PostProcessing.Tagging
open CCVTAC.Console.Settings.Settings
open Startwatch.Library
open TaggingSets

module PostProcessor =

    let private collectionMetadataRegex =
        Regex(@"(?<=\[)[\w\-]{17,}(?=\]\.info.json)", RegexOptions.Compiled)

    let private isCollectionMetadataMatch (path: string) : bool =
        collectionMetadataRegex.IsMatch path

    let private getCollectionJson workingDirectoryName : Result<CollectionMetadata, string> =
        try
            let fileNames =
                Directory.GetFiles workingDirectoryName
                |> Seq.filter isCollectionMetadataMatch
                |> Set.ofSeq

            if fileNames.Count = 0 then
                Error "No relevant files found."
            elif fileNames.Count > 1 then
                Error "Unexpectedly found more than one relevant file, so none will be processed."
            else
                let fileName = fileNames.Single()
                let json = File.ReadAllText fileName
                match JsonSerializer.Deserialize<CollectionMetadata> json with
                | Null -> Error $"Deserialized collection metadata for \"%s{fileName}\" was null."
                | NonNull parsedData -> Ok parsedData
        with
        | ex -> Error ex.Message

    let private generateTaggingSets directoryName : Result<TaggingSet list, string> =
        try
            let taggingSets = createSets <| Directory.GetFiles directoryName
            if List.isEmpty taggingSets
            then Error $"No tagging sets were created using working directory \"%s{directoryName}\"."
            else Ok taggingSets
        with ex -> Error $"Error reading working files in \"{directoryName}\": %s{ex.Message}"

    let run settings mediaType (printer: Printer) : unit =
        let watch = Watch()
        let workingDirectory = settings.WorkingDirectory

        printer.Info "Starting post-processing..."

        match generateTaggingSets workingDirectory with
        | Error _ ->
            printer.Error $"No tagging sets were generated for directory {workingDirectory}, so tagging cannot be done."
        | Ok taggingSets ->
            let collectionJson =
                match getCollectionJson workingDirectory with
                | Error e ->
                    printer.Debug $"No playlist or channel metadata found: %s{e}"
                    None
                | Ok cm ->
                    printer.Debug "Found playlist/channel metadata."
                    Some cm

            if settings.EmbedImages then
                ImageProcessor.run workingDirectory printer

            match Tagger.run settings taggingSets collectionJson mediaType printer with
            | Ok msg ->
                printer.Info msg
                Renamer.run settings workingDirectory printer
                Mover.run taggingSets collectionJson settings true printer

                let allTaggingSetFiles = taggingSets |> List.collect allFiles
                Deleter.run allTaggingSetFiles collectionJson workingDirectory printer

                match Directories.warnIfAnyFiles workingDirectory 20 with
                | Ok _ -> ()
                | Error err ->
                    printer.Error err
                    printer.Info "Will delete the remaining files..."
                    match Directories.deleteAllFiles workingDirectory 20 with
                    | Ok deletedCount -> printer.Info $"%d{deletedCount} file(s) deleted."
                    | Error e -> printer.Error e
            | Error e ->
                printer.Error($"Tagging error(s) preventing further post-processing: {e}")

        printer.Info $"Post-processing done in %s{watch.ElapsedFriendly}."
