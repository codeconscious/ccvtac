namespace CCVTAC.Main.Downloading

open CCFSharpUtils.Text

module Downloading =

    /// The type of media a media URL can represent.
    type MediaType =
        /// A standard video.
        | Video of Id: string
        /// A short-form, vertically-oriented Shorts video.
        | Short of Id: string
        /// A standard video that is also included on a playlist.
        | PlaylistVideo of VideoId: string * PlaylistId: string
        /// A standard playlist of videos.
        | StandardPlaylist of Id: string
        /// A playlist of videos on an official "Topic" channel.
        | ReleasePlaylist of Id: string
        /// A user's channel.
        | Channel of Id: string

    let typeName mediaType =
        match mediaType with
        | Video _ -> "Video"
        | Short _ -> "Shorts"
        | PlaylistVideo _ -> "Playlist Video"
        | StandardPlaylist _ -> "Standard Playlist"
        | ReleasePlaylist _ -> "Release Playlist"
        | Channel _ -> "Channel"

    let isSingleVideo mediaType =
        match mediaType with
        | Video _ | Short _ | PlaylistVideo _                -> true
        | StandardPlaylist _ | ReleasePlaylist _ | Channel _ -> false

    let isMultiVideo =
        not << isSingleVideo

    type PrimaryUrl = PrimaryUrl of string

    type SupplementaryUrl = SupplementaryUrl of string option

    type MediaUrls = { Primary: PrimaryUrl
                       Metadata: SupplementaryUrl }

    let mediaTypeWithIds url : Result<MediaType,string> =
        match url with
        | Rgx.MatchGroups @"(?<=v=|v\=)([\w-]{11})(?:&list=([\w_-]+))" [videoId; playlistId] ->
            Ok (PlaylistVideo (videoId, playlistId))
        | Rgx.MatchGroups @"^([\w-]{11})$" [id]
        | Rgx.MatchGroups @"(?<=v=|v\\=)([\w-]{11})" [id]
        | Rgx.MatchGroups @"(?<=youtu\.be/)(.{11})" [id] ->
            Ok (Video id)
        | Rgx.MatchGroups @"(?<=youtube\.com/shorts/)([A-Za-z0-9_-]{10}[AEIMQUYcgkosw048])" [id] ->
            Ok (Short id)
        | Rgx.MatchGroups @"(?<=list=)(P[\w\-]+)" [id] ->
            Ok (StandardPlaylist id)
        | Rgx.MatchGroups @"(?<=list=)(O[\w\-]+)" [id] ->
            Ok (ReleasePlaylist id)
        | Rgx.MatchGroups @"((?:www\.)?youtube\.com\/(?:channel\/|c\/|user\/|@)(?:[A-Za-z0-9\-@%\/]+))" [id] ->
            Ok (Channel id)
        | _ ->
            Error $"Unable to determine media type of URL \"{url}\". (Might it contain invalid characters?)"

    let generateDownloadUrl mediaType =
        let fullUrl urlBase id = urlBase + id
        let videoUrl    = fullUrl "https://www.youtube.com/watch?v="
        let shortsUrl   = fullUrl "https://www.youtube.com/shorts/"
        let playlistUrl = fullUrl "https://www.youtube.com/playlist?list="
        let channelUrl  = fullUrl "https://" // For channels, the domain portion is also included in matches.

        match mediaType with
        | Video id -> [videoUrl id]
        | Short id -> [shortsUrl id]
        | PlaylistVideo (vId, pId) -> [videoUrl vId; playlistUrl pId]
        | StandardPlaylist id
        | ReleasePlaylist id -> [playlistUrl id]
        | Channel id -> [channelUrl id]
