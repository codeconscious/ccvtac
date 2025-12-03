namespace CCVTAC.Console.PostProcessing.Tagging

open CCVTAC.Console.Settings.Settings
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.PostProcessing.Tagging
open System

module TagDetection =

    let detectTitle
        (videoData: VideoMetadata)
        (defaultTitle: string option)
        (tagDetectionPatterns: TagDetectionPatterns)
        : string option =

        let detectedTitle =
            Detectors.detectSingle<string> videoData tagDetectionPatterns.Title None

        match detectedTitle, defaultTitle with
        | Some title, _ -> Some title
        | None, Some defaultVal -> Some defaultVal
        | None, None -> None

    let detectArtist
        (videoData: VideoMetadata)
        (defaultArtist: string option)
        (tagDetectionPatterns: TagDetectionPatterns)
        : string option =

        let detectedArtist =
            Detectors.detectSingle<string> videoData tagDetectionPatterns.Artist None

        match detectedArtist, defaultArtist with
        | Some artist, _ -> Some artist
        | None, Some defaultVal -> Some defaultVal
        | None, None -> None

    let detectAlbum
        (videoData: VideoMetadata)
        (defaultAlbum: string option)
        (tagDetectionPatterns: TagDetectionPatterns)
        : string option =

        let detectedAlbum =
            Detectors.detectSingle<string> videoData tagDetectionPatterns.Album None

        match detectedAlbum, defaultAlbum with
        | Some album, _ -> Some album
        | None, Some defaultVal -> Some defaultVal
        | None, None -> None

    let detectComposers
        (videoData: VideoMetadata)
        (tagDetectionPatterns: TagDetectionPatterns)
        : string option =

        Detectors.detectMultiple<string> videoData tagDetectionPatterns.Composer String.Empty "; " |> Some

    let detectReleaseYear
        (videoData: VideoMetadata)
        (defaultYear: uint32 option)
        (tagDetectionPatterns: TagDetectionPatterns)
        : uint32 option =

        let detectedYear =
            Detectors.detectSingle<uint32> videoData tagDetectionPatterns.Year None

        match detectedYear, defaultYear with
        | Some year, _ -> Some year
        | None, Some defaultVal -> Some defaultVal
        | None, None -> None
