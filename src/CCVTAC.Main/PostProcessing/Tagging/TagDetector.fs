namespace CCVTAC.Main.PostProcessing.Tagging

open CCVTAC.Main.Settings.Settings
open CCVTAC.Main.PostProcessing.Tagging

module TagDetection =

    let detectTitle videoData fallback (tagDetectionPatterns: TagDetectionPatterns) : string option =
        let detectedTitle =
            Detectors.detectSingle<string> videoData tagDetectionPatterns.Title None

        match detectedTitle, fallback with
        | Some title, _ -> Some title
        | None, Some title -> Some title
        | None, None -> None

    let detectArtist videoData fallback (tagDetectionPatterns: TagDetectionPatterns) : string option =
        let detectedArtist =
            Detectors.detectSingle<string> videoData tagDetectionPatterns.Artist None

        match detectedArtist, fallback with
        | Some artist, _ -> Some artist
        | None, Some artist -> Some artist
        | None, None -> None

    let detectAlbum videoData fallback (tagDetectionPatterns: TagDetectionPatterns) : string option =
        let detectedAlbum =
            Detectors.detectSingle<string> videoData tagDetectionPatterns.Album None

        match detectedAlbum, fallback with
        | Some album, _ -> Some album
        | None, Some album -> Some album
        | None, None -> None

    let detectComposers videoData (tagDetectionPatterns: TagDetectionPatterns) : string option =
        Detectors.detectMultiple<string> videoData tagDetectionPatterns.Composer None "; "

    let detectReleaseYear videoData fallback (tagDetectionPatterns: TagDetectionPatterns) : uint32 option =
        let detectedYear =
            Detectors.detectSingle<uint32> videoData tagDetectionPatterns.Year None

        match detectedYear, fallback with
        | Some year, _ -> Some year
        | None, Some year -> Some year
        | None, None -> None
