namespace CCVTAC.Console.PostProcessing.Tagging

open System
open System.IO
open System.Text.Json
open CCVTAC.Console
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.PostProcessing.Tagging
open CCVTAC.Console.Downloading.Downloading
open Startwatch.Library
open TaggingSets
open MetadataUtilities

type TaggedFile = TagLib.File

module Tagger =

    let private parseVideoJson taggingSet : Result<VideoMetadata, string> =
        try
            let json = File.ReadAllText taggingSet.JsonFilePath
            try
                match JsonSerializer.Deserialize<VideoMetadata> json with
                | Null -> Error $"Deserialized JSON was null for \"%s{taggingSet.JsonFilePath}\"."
                | NonNull v -> Ok v
            with
            | :? JsonException as ex -> Error $"%s{ex.Message}%s{String.newLine}%s{ex.StackTrace}"
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
        if String.hasNoText imageFilePath then
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
        if userSettings.IgnoreUploadYearUploaders |> List.caseInsensitiveContains videoMetadata.Uploader
        then None
        elif videoMetadata.UploadDate.Length <> 4
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
        let patterns = settings.TagDetectionPatterns

        // Title
        if String.hasText videoData.Track then
            printer.Debug $"• Using metadata title \"%s{videoData.Track}\""
            taggedFile.Tag.Title <- videoData.Track
        else
            match TagDetection.detectTitle videoData (Some videoData.Title) patterns with
            | Some title ->
                printer.Debug $"• Found title \"%s{title}\""
                taggedFile.Tag.Title <- title
            | None -> printer.Debug "No title was found."

        // Artists
        if String.hasText videoData.Artist then
            let metadataArtists = videoData.Artist
            let firstArtist = metadataArtists.Split([|", "|], StringSplitOptions.None)[0]
            let diffSummary =
                if firstArtist = metadataArtists
                then String.Empty
                else $" (extracted from \"%s{metadataArtists}\")"
            taggedFile.Tag.Performers <- [| firstArtist |]
            printer.Debug $"• Using metadata artist \"%s{firstArtist}\"%s{diffSummary}"
        else
            match TagDetection.detectArtist videoData None patterns with
            | None -> ()
            | Some artist ->
                printer.Debug $"• Found artist \"%s{artist}\""
                taggedFile.Tag.Performers <- [| artist |]

        // Album
        if String.hasText videoData.Album then
            printer.Debug $"• Using metadata album \"%s{videoData.Album}\""
            taggedFile.Tag.Album <- videoData.Album
        else
            let collectionTitle = collectionData |> Option.map _.Title
            match TagDetection.detectAlbum videoData collectionTitle patterns with
            | None -> ()
            | Some album ->
                printer.Debug $"• Found album \"%s{album}\""
                taggedFile.Tag.Album <- album

        // Composers
        match TagDetection.detectComposers videoData patterns with
        | None -> ()
        | Some composers ->
            printer.Debug $"• Found composer(s) \"%s{composers}\""
            taggedFile.Tag.Composers <- [| composers |]

        // Track number
        match videoData.PlaylistIndex with
        | None -> ()
        | Some (trackNo: uint32) ->
            printer.Debug $"• Using playlist index of %d{trackNo} for track number"
            taggedFile.Tag.Track <- uint32 trackNo

        // Year
        match videoData.ReleaseYear with
        | Some (year: uint32) ->
            printer.Debug $"• Using metadata release year \"%d{year}\""
            taggedFile.Tag.Year <- year
        | None ->
            let defaultYear = releaseYear settings videoData
            match TagDetection.detectReleaseYear videoData defaultYear patterns with
            | None -> ()
            | Some year ->
                printer.Debug $"• Found year \"%d{year}\""
                taggedFile.Tag.Year <- year

        // Comment
        taggedFile.Tag.Comment <- generateComment videoData collectionData

        // Artwork embedding
        match imageFilePath with
        | Some path ->
            if settings.EmbedImages && settings.DoNotEmbedImageUploaders |> List.doesNotContain videoData.Uploader
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

        printer.Debug $"""%s{String.fileLabel (Some "audio") taggingSet.AudioFilePaths.Length} with resource ID %s{taggingSet.ResourceId}"""

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
        (taggingSets: TaggingSet seq)
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
