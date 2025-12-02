namespace CCVTAC.Console.PostProcessing.Tagging

open System
open System.IO
open System.Text.Json
open CCVTAC.Console
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.Downloading.Downloading
open Startwatch.Library
open TaggingSets

type TaggedFile = TagLib.File

module Tagger =

    let private parseVideoJson taggingSet : Result<VideoMetadata, string> =
        try
            let json = File.ReadAllText taggingSet.JsonFilePath
            try
                #nowarn 3265
                let videoData = JsonSerializer.Deserialize<VideoMetadata>(json)
                #warnon 3265

                // TODO: Make this more idiomatic.
                if isNull (box videoData) then Error $"Deserialized JSON was null for \"%s{taggingSet.JsonFilePath}\""
                else Ok videoData
            with
            | :? JsonException as ex -> Error $"%s{ex.Message}%s{newLine}%s{ex.StackTrace}"
        with ex ->
            Error $"Error reading JSON file \"%s{taggingSet.JsonFilePath}\": %s{ex.Message}."

    let private deleteSourceFile taggingSet (printer: Printer) : TaggingSet =
        if taggingSet.AudioFilePaths.Length <= 1 then taggingSet
        else
            let largestFileInfo =
                taggingSet.AudioFilePaths
                |> Seq.map FileInfo
                |> Seq.sortByDescending _.Length
                |> Seq.head

            try
                File.Delete largestFileInfo.FullName
                printer.Debug $"Deleted pre-split source file \"%s{largestFileInfo.Name}\""
                { taggingSet with
                    AudioFilePaths = taggingSet.AudioFilePaths
                                     |> List.except [largestFileInfo.FullName] }
            with ex ->
                printer.Error $"Error deleting pre-split source file \"%s{largestFileInfo.Name}\": %s{ex.Message}"
                taggingSet

    let private writeImage (taggedFile: TaggedFile) imageFilePath (printer: Printer) =
        if hasNoText imageFilePath then
            printer.Error "No image file path was provided, so cannot add an image to the file."
        else
            try
                let pics = Array.zeroCreate<TagLib.IPicture> 1
                pics[0] <- TagLib.Picture imageFilePath
                taggedFile.Tag.Pictures <- pics
                printer.Debug "Image written to file tags OK."
            with ex ->
                printer.Error $"Error writing image to the audio file: %s{ex.Message}"

    let private releaseYear userSettings videoMetadata : uint32 option =
        if userSettings.IgnoreUploadYearUploaders |> caseInsensitiveContains videoMetadata.Uploader
        then None
        else if videoMetadata.UploadDate.Length <> 4
        then None
        else
            match UInt32.TryParse(videoMetadata.UploadDate.Substring(0, 4)) with
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
        if hasText videoData.Track then
            printer.Debug $"• Using metadata title \"%s{videoData.Track}\""
            taggedFile.Tag.Title <- videoData.Track
        else
            match tagDetector.DetectTitle(videoData, videoData.Title) with
            | Some title ->
                printer.Debug $"• Found title \"%s{title}\""
                taggedFile.Tag.Title <- title
            | None -> printer.Debug "No title was found."

        // Artists
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
            printer.Debug $"• Found composer(s) \"%s{composers}\""
            taggedFile.Tag.Composers <- [| composers |]

        // Track number
        match videoData.PlaylistIndex with
        | NullV -> ()
        | NonNullV (trackNo: uint32) ->
            printer.Debug $"• Using playlist index of %d{trackNo} for track number"
            taggedFile.Tag.Track <- uint32 trackNo

        // Year
        match videoData.ReleaseYear with
        | NonNullV (year: uint32) ->
            printer.Debug $"• Using metadata release year \"%d{year}\""
            taggedFile.Tag.Year <- year
        | NullV ->
            let maybeDefaultYear = releaseYear settings videoData
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
            if settings.EmbedImages && settings.DoNotEmbedImageUploaders |> Array.doesNotContain videoData.Uploader
            then
                printer.Info "Embedding artwork."
                writeImage taggedFile path printer
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

        printer.Debug $"%d{taggingSet.AudioFilePaths.Length} audio file(s) with resource ID %s{taggingSet.ResourceId}"

        match parseVideoJson taggingSet with
        | Ok videoData ->
            let finalTaggingSet = deleteSourceFile taggingSet printer

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
