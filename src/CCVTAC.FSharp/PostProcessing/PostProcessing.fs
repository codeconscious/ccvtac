namespace CCVTAC.Console.PostProcessing

open CCVTAC.Console
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.PostProcessing.Tagging
open CCVTAC.Console.Settings.Settings
open System.IO
open System.Linq
open System.Text.Json
open System.Text.RegularExpressions
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

            if Seq.isEmpty fileNames then
                Error "No relevant files found."
            elif Seq.hasMultiple fileNames then
                Error "Unexpectedly found more than one relevant file, so none will be processed."
            else
                let fileName = fileNames.Single()
                let json = File.ReadAllText fileName
                match JsonSerializer.Deserialize<CollectionMetadata> json with
                | Null -> Error $"Deserialized collection metadata for \"%s{fileName}\" was null."
                | NonNull parsedData -> Ok parsedData
        with
        | ex -> Error ex.Message

    let private generateTaggingSets dir : Result<TaggingSet list, string> =
        try
            let taggingSets = createSets <| Directory.GetFiles dir
            if List.isEmpty taggingSets
            then Error $"No tagging sets were created using files in working directory \"%s{dir}\". Are all file extensions correct?"
            else Ok taggingSets
        with exn ->
            Error $"Error reading working files in \"{dir}\": %s{exn.Message}"

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

                match Directories.warnIfAnyFiles 20 workingDirectory with
                | Ok _ -> ()
                | Error err ->
                    printer.Error err
                    printer.Info "Will delete the remaining files..."
                    match Directories.deleteAllFiles workingDirectory with
                    | Ok results -> Directories.printDeletionResults printer results
                    | Error e -> printer.Error e
            | Error e ->
                printer.Error($"Tagging error(s) preventing further post-processing: {e}")

        printer.Info $"Post-processing done in %s{watch.ElapsedFriendly}."
