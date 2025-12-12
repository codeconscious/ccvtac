namespace CCVTAC.Console.Downloading

open System.Text.RegularExpressions

module Downloading =

    type MediaType =
        | Video of Id: string
        | PlaylistVideo of VideoId: string * PlaylistId: string
        | StandardPlaylist of Id: string
        | ReleasePlaylist of Id: string
        | Channel of Id: string

    type PrimaryUrl = PrimaryUrl of string
    type SupplementaryUrl = SupplementaryUrl of string option

    type Urls = { Primary: PrimaryUrl
                  Metadata: SupplementaryUrl }

    let private (|RegexMatch|_|) pattern input =
        match Regex.Match(input, pattern) with
        | m when m.Success -> Some (List.tail [for g in m.Groups -> g.Value])
        | _ -> None

    let mediaTypeWithIds url : Result<MediaType,string> =
        match url with
        | RegexMatch @"(?<=v=|v\=)([\w-]{11})(?:&list=([\w_-]+))" [videoId; playlistId] ->
            Ok (PlaylistVideo (videoId, playlistId))
        | RegexMatch @"^([\w-]{11})$" [id]
        | RegexMatch @"(?<=v=|v\\=)([\w-]{11})" [id]
        | RegexMatch @"(?<=youtu\.be/)(.{11})" [id] ->
            Ok (Video id)
        | RegexMatch @"(?<=list=)(P[\w\-]+)" [id] ->
            Ok (StandardPlaylist id)
        | RegexMatch @"(?<=list=)(O[\w\-]+)" [id] ->
            Ok (ReleasePlaylist id)
        | RegexMatch @"((?:www\.)?youtube\.com\/(?:channel\/|c\/|user\/|@)(?:[A-Za-z0-9\-@%\/]+))" [id] ->
            Ok (Channel id)
        | _ ->
            Error $"Unable to determine media type of URL \"{url}\". (Might it contain invalid characters?)"

    let generateDownloadUrl mediaType =
        let fullUrl urlBase id = urlBase + id
        let videoUrl = fullUrl "https://www.youtube.com/watch?v="
        let playlistUrl = fullUrl "https://www.youtube.com/playlist?list="
        let channelUrl = fullUrl "https://" // For channels, the domain portion is also matched.

        match mediaType with
        | Video id -> [videoUrl id]
        | PlaylistVideo (vId, pId) -> [videoUrl vId; playlistUrl pId]
        | StandardPlaylist id | ReleasePlaylist id -> [playlistUrl id]
        | Channel id -> [channelUrl id]
