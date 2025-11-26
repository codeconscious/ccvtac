namespace CCVTAC.Console.PostProcessing

open System.IO
open System.Linq
open System.Text.Json
open System.Text.RegularExpressions
open System.Collections.Immutable
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.PostProcessing.Tagging
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console
open CCVTAC.Console.Downloading
open CCVTAC.Console.Downloading.Downloading
open Startwatch.Library

module PostProcessor =

    // let internal AudioExtensions =
    //     [| ".aac"; ".alac"; ".flac"; ".m4a"; ".mp3"; ".ogg"; ".vorbis"; ".opus"; ".wav" |]

    let private collectionMetadataRegex =
        Regex(@"(?<=

\[)[\w\-]{17,}(?=\]

\.info.json)", RegexOptions.Compiled)

    let private getCollectionMetadataMatches (path: string) =
        collectionMetadataRegex.IsMatch path

    let private GetCollectionJson (workingDirectory: string) : Result<CollectionMetadata, string> =
        try
            let fileNames =
                Directory.GetFiles workingDirectory
                |> Seq.filter getCollectionMetadataMatches
                |> Set.ofSeq

            if fileNames.Count = 0 then
                Error "No relevant files found."
            elif fileNames.Count > 1 then
                Error "Unexpectedly found more than one relevant file, so none will be processed."
            else
                let fileName = fileNames.Single()
                let json = File.ReadAllText fileName
                #nowarn 3265
                let collectionData = JsonSerializer.Deserialize<CollectionMetadata>(json)
                #warnon 3265
                if isNull (box collectionData) then
                    Error "Deserialized collection metadata was null."
                else
                    Ok collectionData
        with ex ->
            Error ex.Message

    let private GenerateTaggingSets (directory: string) : Result<TaggingSet list, string> =
        try
            let files = Directory.GetFiles directory
            let taggingSets = TaggingSet.CreateSets files
            if taggingSets.Any()
            then Ok taggingSets
            else Error (sprintf "No tagging sets were created using working directory \"%s\"." directory)
        with
            | :? DirectoryNotFoundException ->
                Error (sprintf "Directory \"%s\" does not exist." directory)
            | ex ->
                Error (sprintf "Error reading working directory files: %s" ex.Message)

    let Run (settings: UserSettings) (mediaType: MediaType) (printer: Printer) : unit =
        let watch = Watch()
        let workingDirectory = settings.WorkingDirectory

        printer.Info "Starting post-processing..."

        match GenerateTaggingSets workingDirectory with
        | Error _ ->
            printer.Error "No tagging sets were generated, so tagging cannot be done."
        | Ok taggingSets ->
            let collectionJsonResult = GetCollectionJson workingDirectory

            let collectionJsonOpt =
                match collectionJsonResult with
                | Error e ->
                    printer.Debug $"No playlist or channel metadata found: %s{e}"
                    None
                | Ok cm ->
                    printer.Debug "Found playlist/channel metadata."
                    Some cm

            if settings.EmbedImages
            then ImageProcessor.Run workingDirectory printer
            else ()

            match Tagger.Run settings taggingSets collectionJsonOpt mediaType printer with
            | Ok msg ->
                printer.Info msg
                Renamer.Run settings workingDirectory printer
                Mover.Run taggingSets collectionJsonOpt settings true printer

                let taggingSetFileNames =
                    taggingSets
                    |> Seq.collect _.AllFiles
                    |> Seq.toList

                Deleter.run taggingSetFileNames collectionJsonOpt workingDirectory printer

                match Directories.warnIfAnyFiles workingDirectory 20 with
                | Ok _ -> ()
                | Error firstErr ->
                    printer.FirstError firstErr
                    printer.Info "Will delete the remaining files..."
                    match Directories.deleteAllFiles workingDirectory 20 with
                    | Ok deletedCount -> printer.Info $"%d{deletedCount} file(s) deleted."
                    | Error e -> printer.FirstError e
            | Error e ->
                printer.Error($"Tagging error(s) preventing further post-processing: {e}")

        printer.Info $"Post-processing done in %s{watch.ElapsedFriendly}."
