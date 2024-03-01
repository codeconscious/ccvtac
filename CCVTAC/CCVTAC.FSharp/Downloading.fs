namespace CCVTAC.FSharp

module public Downloading =
    open System.Text.RegularExpressions

    type MediaType =
        | Video of string
        | PlaylistVideo of string * string
        | StandardPlaylist of string
        | ReleasePlaylist of string
        | Channel of string

    // This active recognizer will not work if the parameter order is switched.
    let private (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        match m with
            | m when m.Success -> Some(List.tail [ for g in m.Groups -> g.Value ])
            | _ -> None

    let mediaTypeWithIds (url:string) =
        match url with
        | Regex @"(?<=v=|v\=)([\w-]{11})(?:&list=([\w_-]+))" [ videoId ; playlistId ]
            -> Ok (PlaylistVideo (videoId, playlistId))
        | Regex @"^([\w-]{11})$" [ id ] -> Ok (Video id)
        | Regex @"(?<=v=|v\\=)([\w-]{11})" [ id ] -> Ok (Video id)
        | Regex @"(?<=youtu\.be/)(.{11})" [ id ] -> Ok (Video id)
        | Regex @"(?<=list=)(P[\w\-]+)" [ id ] -> Ok (StandardPlaylist id)
        | Regex @"(?<=list=)(O[\w\-]+)" [ id ] -> Ok (ReleasePlaylist id)
        | Regex @"((?:www\.)?youtube\.com\/(?:channel\/|c\/|user\/|@)(?:[\w\-]+))" [ id ] -> Ok (Channel id)
        | _ -> Error("Unable to determine the media type of the URL.")

    let downloadUrls mediaType =
        let fullUrl urlBase id = urlBase + id
        let videoUrl = fullUrl "https://www.youtube.com/watch?v="
        let playlistUrl = fullUrl "https://www.youtube.com/playlist?list="
        let channelUrl = fullUrl "https://" // For channels, the entire domain is also matched.

        match mediaType with
        | Video id -> [videoUrl id]
        | PlaylistVideo (vId, pId) -> [videoUrl vId; playlistUrl pId]
        | StandardPlaylist id | ReleasePlaylist id -> [playlistUrl id]
        | Channel id -> [channelUrl id]
