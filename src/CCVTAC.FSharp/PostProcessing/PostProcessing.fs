namespace CCVTAC.Console.PostProcessing

open System
open System.IO
open System.Linq
open System.Text.Json
open System.Text.RegularExpressions
open System.Collections.Immutable
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.PostProcessing.Tagging
open CCVTAC.FSharp.Settings

module PostProcessor =

    let internal AudioExtensions =
        [| ".aac"; ".alac"; ".flac"; ".m4a"; ".mp3"; ".ogg"; ".vorbis"; ".opus"; ".wav" |]

    let private collectionMetadataRegex =
        Regex(@"(?<=

\[)[\w\-]{17,}(?=\]

\.info.json)", RegexOptions.Compiled)

    let private getCollectionMetadataMatches (path: string) =
        collectionMetadataRegex.IsMatch(path)

    let Run (settings: UserSettings) (mediaType: MediaType) (printer: Printer) : unit =
        let watch = Watch()
        let workingDirectory = settings.WorkingDirectory

        printer.Info "Starting post-processing..."

        match GenerateTaggingSets workingDirectory with
        | Error err ->
            printer.Error "No tagging sets were generated, so tagging cannot be done."
        | Ok taggingSets ->
            let collectionJsonResult = GetCollectionJson workingDirectory

            let collectionJsonOpt =
                match collectionJsonResult with
                | Error e ->
                    printer.Debug (sprintf "No playlist or channel metadata found: %s" e)
                    None
                | Ok cm ->
                    printer.Debug "Found playlist/channel metadata."
                    Some cm

            if settings.EmbedImages then
                ImageProcessor.Run(workingDirectory, printer)

            match Tagger.Run(settings, taggingSets, collectionJsonOpt, mediaType, printer) with
            | Ok msg ->
                printer.Info msg
                Renamer.Run(settings, workingDirectory, printer)
                Mover.Run(taggingSets, collectionJsonOpt, settings, true, printer)

                let taggingSetFileNames =
                    taggingSets
                    |> Seq.collect (fun s -> s.AllFiles :?> seq<string>)
                    |> Seq.toList

                Deleter.Run(taggingSetFileNames, collectionJsonOpt, workingDirectory, printer)

                match Directories.WarnIfAnyFiles(workingDirectory, 20) with
                | Error firstErr ->
                    printer.FirstError(firstErr)
                    printer.Info "Will delete the remaining files..."
                    match Directories.DeleteAllFiles(workingDirectory, 20) with
                    | Ok deletedCount -> printer.Info (sprintf "%d file(s) deleted." deletedCount)
                    | Error e -> printer.FirstError(e)
                | Ok _ -> ()
            | Error errs ->
                printer.Errors("Tagging error(s) preventing further post-processing: ", Error errs)

        printer.Info (sprintf "Post-processing done in %s." watch.ElapsedFriendly)

    let private GetCollectionJson (workingDirectory: string) : Result<CollectionMetadata, string> =
        try
            let fileNames =
                Directory.GetFiles(workingDirectory)
                |> Seq.filter getCollectionMetadataMatches
                |> Seq.toImmutableHashSet

            if fileNames.Count = 0 then
                Error "No relevant files found."
            elif fileNames.Count > 1 then
                Error "Unexpectedly found more than one relevant file, so none will be processed."
            else
                let fileName = fileNames.Single()
                let json = File.ReadAllText(fileName)
                let collectionData = JsonSerializer.Deserialize<CollectionMetadata>(json)
                if isNull (box collectionData) then
                    Error "Deserialized collection metadata was null."
                else
                    Ok collectionData
        with ex ->
            Error ex.Message

    let private GenerateTaggingSets (directory: string) : Result<ImmutableList<TaggingSet>, string> =
        try
            let files = Directory.GetFiles(directory)
            let taggingSets = TaggingSet.CreateSets(files)
            if taggingSets.Any() then Ok taggingSets
            else Error (sprintf "No tagging sets were created using working directory \"%s\"." directory)
        with :? DirectoryNotFoundException ->
            Error (sprintf "Directory \"%s\" does not exist." directory)
        with ex ->
            Error (sprintf "Error reading working directory files: %s" ex.Message)
