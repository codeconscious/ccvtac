namespace CCVTAC.Console.PostProcessing.Tagging

open System
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.PostProcessing.Tagging

/// Provides methods to search for specific tag field data (artist, album, etc.) within video metadata.
type TagDetector(tagDetectionPatterns: TagDetectionPatterns) =
    /// Detection patterns for various metadata fields
    member private _.Patterns = tagDetectionPatterns

    /// Detects the title from video metadata
    member this.DetectTitle(videoData: VideoMetadata, ?defaultTitle: string) : string option =
        let detectedTitle =
            Detectors.detectSingle<string> videoData this.Patterns.Title None

        match detectedTitle, defaultTitle with
        | Some title, _ -> Some title
        | None, Some defaultVal -> Some defaultVal
        | None, None -> None

    /// Detects the artist from video metadata
    member this.DetectArtist(videoData: VideoMetadata, ?defaultArtist: string) : string option =
        let detectedArtist =
            Detectors.detectSingle<string> videoData this.Patterns.Artist None

        match detectedArtist, defaultArtist with
        | Some artist, _ -> Some artist
        | None, Some defaultVal -> Some defaultVal
        | None, None -> None

    /// Detects the album from video metadata
    member this.DetectAlbum(videoData: VideoMetadata, defaultAlbum: string option) : string option =
        let detectedAlbum =
            Detectors.detectSingle<string> videoData this.Patterns.Album None

        match detectedAlbum, defaultAlbum with
        | Some album, _ -> Some album
        | None, Some defaultVal -> Some defaultVal
        | None, None -> None

    /// Detects composers from video metadata
    member this.DetectComposers(videoData: VideoMetadata) : string option =
        Detectors.detectMultiple<string> videoData this.Patterns.Composer (String.Empty) "; " |> Some

    /// Detects the release year from video metadata
    member this.DetectReleaseYear(videoData: VideoMetadata, defaultYear: uint32 option) : uint32 option =
        let detectedYear =
            Detectors.detectSingle<uint32> videoData this.Patterns.Year None

        match detectedYear, defaultYear with
        | Some year, _ -> Some year
        | None, Some defaultVal -> Some defaultVal
        | None, None -> None
