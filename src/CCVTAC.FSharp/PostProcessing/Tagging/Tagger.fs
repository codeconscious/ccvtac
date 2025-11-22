namespace CCVTAC.Console.PostProcessing.Tagging

open System
open System.IO
open System.Text.Json
open System.Linq
open CCVTAC.FSharp.Downloading
open CCVTAC.FSharp.Settings
open CCVTAC.Console.ExternalTools
open TagLib

type TaggedFile = TagLib.File

module Tagger =

    let private watchFriendly (watch: System.Diagnostics.Stopwatch) =
        if watch.IsRunning then watch.Stop()
        let ts = watch.Elapsed
        if ts.TotalHours >= 1.0 then sprintf "%d:%02d:%02d" (int ts.TotalHours) ts.Minutes ts.Seconds
        elif ts.TotalMinutes >= 1.0 then sprintf "%d:%02d" ts.Minutes ts.Seconds
        else sprintf "%d ms" ts.TotalMilliseconds

    let Run
        (settings: UserSettings)
        (taggingSets: seq<TaggingSet>)
        (collectionJson: CollectionMetadata option)
        (mediaType: MediaType)
        (printer: Printer)
        : Result<string, string> =
        printer.Debug "Adding file tags..."
        let watch = System.Diagnostics.Stopwatch.StartNew()
        let embedImages = settings.EmbedImages && (mediaType.IsVideo || mediaType.IsPlaylistVideo)

        for taggingSet in taggingSets do
            ProcessSingleTaggingSet settings taggingSet collectionJson embedImages printer

        Ok (sprintf "Tagging done in %s." (watchFriendly watch))

    and private ProcessSingleTaggingSet
        (settings: UserSettings)
        (taggingSet: TaggingSet)
        (collectionJson: CollectionMetadata option)
        (embedImages: bool)
        (printer: Printer)
        =
        printer.Debug (sprintf "%d audio file(s) with resource ID \"%s\"" taggingSet.AudioFilePaths.Count taggingSet.ResourceId)

        match ParseVideoJson taggingSet with
        | Error err ->
            printer.Errors (sprintf "Error deserializing video metadata from \"%s\":" taggingSet.JsonFilePath) (Error err)
            ()
        | Ok videoData ->
            let finalTaggingSet = DeleteSourceFile taggingSet printer

            let maybeImagePath =
                if embedImages && finalTaggingSet.AudioFilePaths.Count = 1 then
                    finalTaggingSet.ImageFilePath
                else
                    null

            for audioPath in finalTaggingSet.AudioFilePaths do
                try
                    TagSingleFile(settings, videoData, audioPath, maybeImagePath, collectionJson, printer)
                with ex ->
                    printer.Error (sprintf "Error tagging file: %s" ex.Message)

    and private TagSingleFile
        (settings: UserSettings)
        (videoData: VideoMetadata)
        (audioFilePath: string)
        (imageFilePath: string)
        (collectionData: CollectionMetadata option)
        (printer: Printer)
        =
        let audioFileName = Path.GetFileName audioFilePath
        printer.Debug (sprintf "Current audio file: \"%s\"" audioFileName)

        use taggedFile = TaggedFile.Create(audioFilePath)
        let tagDetector = TagDetector(settings.TagDetectionPatterns)

        // Title
        match videoData.Track with
        | null ->
            let title = tagDetector.DetectTitle(videoData, videoData.Title)
            printer.Debug (sprintf "• Found title \"%s\"" title)
            taggedFile.Tag.Title <- title
        | metadataTitle ->
            printer.Debug (sprintf "• Using metadata title \"%s\"" metadataTitle)
            taggedFile.Tag.Title <- metadataTitle

        // Artist / Performers
        if not (String.IsNullOrWhiteSpace(videoData.Artist)) then
            let metadataArtists = videoData.Artist
            let firstArtist = metadataArtists.Split([|", "|], StringSplitOptions.None).[0]
            let diffSummary = if firstArtist = metadataArtists then "" else sprintf " (extracted from \"%s\")" metadataArtists
            taggedFile.Tag.Performers <- [| firstArtist |]
            printer.Debug (sprintf "• Using metadata artist \"%s\"%s" firstArtist diffSummary)
        else
            match tagDetector.DetectArtist(videoData) with
            | null -> ()
            | artist ->
                printer.Debug (sprintf "• Found artist \"%s\"" artist)
                taggedFile.Tag.Performers <- [| artist |]

        // Album
        if not (String.IsNullOrWhiteSpace(videoData.Album)) then
            printer.Debug (sprintf "• Using metadata album \"%s\"" videoData.Album)
            taggedFile.Tag.Album <- videoData.Album
        else
            match tagDetector.DetectAlbum(videoData, collectionData |> Option.map (fun c -> c.Title) |> Option.toObj) with
            | null -> ()
            | album ->
                printer.Debug (sprintf "• Found album \"%s\"" album)
                taggedFile.Tag.Album <- album

        // Composers
        match tagDetector.DetectComposers(videoData) with
        | null -> ()
        | composers ->
            printer.Debug (sprintf "• Found composer(s) \"%s\"" composers)
            taggedFile.Tag.Composers <- [| composers |]

        // Track number
        match videoData.PlaylistIndex with
        | null -> ()
        | trackNo ->
            printer.Debug (sprintf "• Using playlist index of %d for track number" trackNo)
            taggedFile.Tag.Track <- uint32 trackNo

        // Year
        if videoData.ReleaseYear <> null then
            printer.Debug (sprintf "• Using metadata release year \"%d\"" videoData.ReleaseYear)
            taggedFile.Tag.Year <- videoData.ReleaseYear
        else
            let maybeDefaultYear =
                let rec GetAppropriateReleaseDateIfAny (settings: UserSettings) (videoData: VideoMetadata) =
                    if settings.IgnoreUploadYearUploaders |> Option.isSome &&
                       settings.IgnoreUploadYearUploaders.Value.Contains(videoData.Uploader, StringComparer.OrdinalIgnoreCase) then
                        None
                    else
                        if String.IsNullOrEmpty videoData.UploadDate then None
                        else
                            let prefix = if videoData.UploadDate.Length >= 4 then videoData.UploadDate.Substring(0,4) else ""
                            match UInt16.TryParse prefix with
                            | true, parsed -> Some parsed
                            | _ -> None
                GetAppropriateReleaseDateIfAny settings videoData

            match tagDetector.DetectReleaseYear(videoData, maybeDefaultYear) with
            | null -> ()
            | year ->
                printer.Debug (sprintf "• Found year \"%d\"" year)
                taggedFile.Tag.Year <- year

        // Comment
        taggedFile.Tag.Comment <- videoData.GenerateComment(collectionData)

        // Artwork embedding
        if settings.EmbedImages &&
           (not (settings.DoNotEmbedImageUploaders.Contains(videoData.Uploader))) &&
           (not (String.IsNullOrWhiteSpace imageFilePath)) then
            printer.Info "Embedding artwork."
            WriteImage(taggedFile, imageFilePath, printer)
        else
            printer.Debug "Skipping artwork embedding."

        taggedFile.Save()
        printer.Debug (sprintf "Wrote tags to \"%s\"." audioFileName)

    and private ParseVideoJson (taggingSet: TaggingSet) : Result<VideoMetadata, string> =
        try
            let json = File.ReadAllText taggingSet.JsonFilePath
            try
                let videoData = JsonSerializer.Deserialize<VideoMetadata>(json)
                if isNull (box videoData) then Error (sprintf "Deserialized JSON was null for \"%s\"" taggingSet.JsonFilePath)
                else Ok videoData
            with
            | :? JsonException as ex -> Error (sprintf "%s%s%s" ex.Message Environment.NewLine ex.StackTrace)
        with ex ->
            Error (sprintf "Error reading JSON file \"%s\": %s." taggingSet.JsonFilePath ex.Message)

    and private DeleteSourceFile (taggingSet: TaggingSet) (printer: Printer) : TaggingSet =
        if taggingSet.AudioFilePaths.Count <= 1 then taggingSet
        else
            let largestFileInfo =
                taggingSet.AudioFilePaths
                |> Seq.map (fun fn -> FileInfo(fn))
                |> Seq.sortByDescending (fun fi -> fi.Length)
                |> Seq.head

            try
                File.Delete largestFileInfo.FullName
                printer.Debug (sprintf "Deleted pre-split source file \"%s\"" largestFileInfo.Name)
                { taggingSet with AudioFilePaths = taggingSet.AudioFilePaths.Remove(largestFileInfo.FullName) }
            with ex ->
                printer.Error (sprintf "Error deleting pre-split source file \"%s\": %s" largestFileInfo.Name ex.Message)
                taggingSet

    and private WriteImage (taggedFile: TaggedFile) (imageFilePath: string) (printer: Printer) =
        if String.IsNullOrWhiteSpace imageFilePath then
            printer.Error "No image file path was provided, so cannot add an image to the file."
        else
            try
                let pics = Array.zeroCreate<TagLib.IPicture> 1
                pics.[0] <- TagLib.Picture(imageFilePath)
                taggedFile.Tag.Pictures <- pics
                printer.Debug "Image written to file tags OK."
            with ex ->
                printer.Error (sprintf "Error writing image to the audio file: %s" ex.Message)
