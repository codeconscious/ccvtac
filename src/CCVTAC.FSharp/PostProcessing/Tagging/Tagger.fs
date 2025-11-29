namespace CCVTAC.Console.PostProcessing.Tagging

open System
open System.IO
open System.Text.Json
open CCVTAC.Console
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.Downloading.Downloading
open Startwatch.Library

type TaggedFile = TagLib.File

module Tagger =

    let private watchFriendly (watch: System.Diagnostics.Stopwatch) =
        if watch.IsRunning then watch.Stop()
        let ts = watch.Elapsed
        if ts.TotalHours >= 1.0 then sprintf "%d:%02d:%02d" (int ts.TotalHours) ts.Minutes ts.Seconds
        elif ts.TotalMinutes >= 1.0 then sprintf "%d:%02d" ts.Minutes ts.Seconds
        else sprintf "%A ms" ts.TotalMilliseconds // TODO: %d didn't work

    let private parseVideoJson (taggingSet: TaggingSet) : Result<VideoMetadata, string> =
        try
            let json = File.ReadAllText taggingSet.JsonFilePath
            try
                #nowarn 3265
                let videoData = JsonSerializer.Deserialize<VideoMetadata>(json)
                #warnon 3265

                if isNull (box videoData) then Error (sprintf "Deserialized JSON was null for \"%s\"" taggingSet.JsonFilePath)
                else Ok videoData
            with
            | :? JsonException as ex -> Error (sprintf "%s%s%s" ex.Message Environment.NewLine ex.StackTrace)
        with ex ->
            Error (sprintf "Error reading JSON file \"%s\": %s." taggingSet.JsonFilePath ex.Message)


    let private DeleteSourceFile (taggingSet: TaggingSet) (printer: Printer) : TaggingSet =
        if taggingSet.AudioFilePaths.Length <= 1 then taggingSet
        else
            let largestFileInfo =
                taggingSet.AudioFilePaths
                |> Seq.map (fun fn -> FileInfo(fn))
                |> Seq.sortByDescending (fun fi -> fi.Length)
                |> Seq.head

            try
                File.Delete largestFileInfo.FullName
                printer.Debug (sprintf "Deleted pre-split source file \"%s\"" largestFileInfo.Name)
                { taggingSet with AudioFilePaths = taggingSet.AudioFilePaths |> List.except [largestFileInfo.FullName] }
            with ex ->
                printer.Error (sprintf "Error deleting pre-split source file \"%s\": %s" largestFileInfo.Name ex.Message)
                taggingSet

    let private WriteImage (taggedFile: TaggedFile) (imageFilePath: string) (printer: Printer) =
        if hasNoText imageFilePath then
            printer.Error "No image file path was provided, so cannot add an image to the file."
        else
            try
                let pics = Array.zeroCreate<TagLib.IPicture> 1
                pics[0] <- TagLib.Picture imageFilePath
                taggedFile.Tag.Pictures <- pics
                printer.Debug "Image written to file tags OK."
            with ex ->
                printer.Error (sprintf "Error writing image to the audio file: %s" ex.Message)


    /// If the supplied video uploader is specified in the settings, returns the video's upload year.
    /// Otherwise, returns null (Nullable<uint16>).
    let releaseYear (settings: UserSettings) (videoData: VideoMetadata) : uint32 option =
        if settings.IgnoreUploadYearUploaders |> caseInsensitiveContains videoData.Uploader
        then None
        else if videoData.UploadDate.Length < 4
        then None
        else
            let yearStr = videoData.UploadDate.Substring(0, 4)
            match UInt32.TryParse yearStr with
            | true, parsed -> Some parsed
            | _ -> None

    let private tagSingleFile
        (settings: UserSettings)
        (videoData: VideoMetadata)
        (audioFilePath: string)
        (imageFilePath: string option)
        (collectionData: CollectionMetadata option)
        (printer: Printer)
        =
        let audioFileName = Path.GetFileName audioFilePath
        printer.Debug $"Current audio file: \"%s{audioFileName}\""

        use taggedFile = TaggedFile.Create audioFilePath
        let tagDetector = TagDetector settings.TagDetectionPatterns

        // Title
        // match videoData.Track with
        // | NonNull (metadataTitle: string) ->
        //     printer.Debug (sprintf "• Using metadata title \"%s\"" metadataTitle)
        //     taggedFile.Tag.Title <- metadataTitle
        // | Null ->
        //     let title = tagDetector.DetectTitle(videoData, videoData.Title)
        //     printer.Debug (sprintf "• Found title \"%s\"" title)
        //     taggedFile.Tag.Title <- title
        if hasText videoData.Track then
            printer.Debug $"• Using metadata title \"%s{videoData.Track}\""
            taggedFile.Tag.Title <- videoData.Track
        else
            match tagDetector.DetectTitle(videoData, videoData.Title) with
            | Some title ->
                printer.Debug $"• Found title \"%s{title}\""
                taggedFile.Tag.Title <- title
            | None -> printer.Debug "No title was found."

        // Artist / Performers
        if hasText videoData.Artist then
            let metadataArtists = videoData.Artist
            let firstArtist = metadataArtists.Split([|", "|], StringSplitOptions.None)[0]
            let diffSummary =
                if firstArtist = metadataArtists
                then String.Empty
                else $" (extracted from \"%s{metadataArtists}\")"
            taggedFile.Tag.Performers <- [| firstArtist |]
            printer.Debug $"• Using metadata artist \"%s{firstArtist}\"%s{diffSummary}"
        else
            match tagDetector.DetectArtist videoData with
            | None -> ()
            | Some artist ->
                printer.Debug $"• Found artist \"%s{artist}\""
                taggedFile.Tag.Performers <- [| artist |]

        // Album
        if hasText videoData.Album then
            printer.Debug $"• Using metadata album \"%s{videoData.Album}\""
            taggedFile.Tag.Album <- videoData.Album
        else
            let collectionTitle = collectionData |> Option.map _.Title
            match tagDetector.DetectAlbum(videoData, collectionTitle) with
            | None -> ()
            | Some album ->
                printer.Debug $"• Found album \"%s{album}\""
                taggedFile.Tag.Album <- album

        // Composers
        match tagDetector.DetectComposers videoData with
        | None -> ()
        | Some composers ->
            printer.Debug (sprintf "• Found composer(s) \"%s\"" composers)
            taggedFile.Tag.Composers <- [| composers |]

        // Track number
        match videoData.PlaylistIndex with
        | NullV -> ()
        | NonNullV (trackNo: uint32) ->
            printer.Debug (sprintf "• Using playlist index of %d for track number" trackNo)
            taggedFile.Tag.Track <- uint32 trackNo

        // Year
        match videoData.ReleaseYear with
        | NonNullV (year: uint32) ->
            printer.Debug $"• Using metadata release year \"%d{year}\""
            taggedFile.Tag.Year <- year
        | NullV ->
            let maybeDefaultYear =
                // let rec GetAppropriateReleaseDateIfAny (settings: UserSettings) (videoData: VideoMetadata) =
                //     if settings.IgnoreUploadYearUploaders.Contains(videoData.Uploader, StringComparer.OrdinalIgnoreCase)
                //     then
                //         None
                //     else
                //         if String.IsNullOrEmpty videoData.UploadDate then None
                //         else
                //             let prefix = if videoData.UploadDate.Length >= 4 then videoData.UploadDate.Substring(0,4) else ""
                //             match UInt16.TryParse prefix with
                //             | true, parsed -> Some parsed
                //             | _ -> None
                releaseYear settings videoData

            match tagDetector.DetectReleaseYear(videoData, maybeDefaultYear) with
            | None -> ()
            | Some year ->
                printer.Debug $"• Found year \"%d{year}\""
                taggedFile.Tag.Year <- year

        // Comment
        taggedFile.Tag.Comment <- videoData.GenerateComment collectionData

        // Artwork embedding
        match imageFilePath with
        | Some path ->
            if settings.EmbedImages &&
                settings.DoNotEmbedImageUploaders |> Array.doesNotContain videoData.Uploader
            then
                printer.Info "Embedding artwork."
                WriteImage taggedFile path printer
            else
                printer.Debug "Skipping artwork embedding."
        | None ->
            printer.Debug "Skipping artwork embedding."

        taggedFile.Save()
        printer.Debug $"Wrote tags to \"%s{audioFileName}\"."

    let private processTaggingSet
        (settings: UserSettings)
        (taggingSet: TaggingSet)
        (collectionJson: CollectionMetadata option)
        (embedImages: bool)
        (printer: Printer)
        : unit
        =

        printer.Debug $"%d{taggingSet.AudioFilePaths.Length} audio file(s) with resource ID \"%s{taggingSet.ResourceId}\""

        match parseVideoJson taggingSet with
        | Ok videoData ->
            let finalTaggingSet = DeleteSourceFile taggingSet printer

            let imagePath =
                if embedImages && List.isNotEmpty finalTaggingSet.AudioFilePaths then
                    Some finalTaggingSet.ImageFilePath
                else
                    None

            for audioPath in finalTaggingSet.AudioFilePaths do
                try
                    tagSingleFile settings videoData audioPath imagePath collectionJson printer
                with ex ->
                    printer.Error $"Error tagging file: %s{ex.Message}"
        | Error err ->
            printer.Error $"Error deserializing video metadata from \"%s{taggingSet.JsonFilePath}\": {err}"

    let run
        (settings: UserSettings)
        (taggingSets: seq<TaggingSet>)
        (collectionJson: CollectionMetadata option)
        (mediaType: MediaType)
        (printer: Printer)
        : Result<string, string> =

        printer.Debug "Adding file tags..."
        let watch = Watch()

        let embedImages = settings.EmbedImages && (mediaType.IsVideo || mediaType.IsPlaylistVideo)

        for taggingSet in taggingSets do
            processTaggingSet settings taggingSet collectionJson embedImages printer

        Ok $"Tagging done in %s{watch.ElapsedFriendly}."
