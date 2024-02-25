namespace CCVTAC.FSharp

module public Downloading =
    open System.Text.RegularExpressions

    type MediaType =
        | Video
        | PlaylistVideo
        | StandardPlaylist
        | ReleasePlaylist
        | Channel

    // type DownloadType =
    //     | MediaType of MediaType
    //     | Metadata

    // This active recognizer will not work with the parameter order is switched.
    let private (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        match m with
            | m when m.Success -> Some(List.tail [ for g in m.Groups -> g.Value ])
            | _ -> None

    let private mediaTypeWithIds (textUrl:string) =
        match textUrl with
        | Regex @"(?<=v=|v\=)([\w-]{11})(?:&list=([\w_-]+))" [ id ; playlistId ]
            -> Some (PlaylistVideo, [id; playlistId])
        | Regex @"^([\w-]{11})$" [ id ] -> Some (Video, [id])
        | Regex @"(?<=v=|v\\=)([\w-]{11})" [ id ] -> Some (Video, [id])
        | Regex @"(?<=youtu\.be/)(.{11})" [ id ] -> Some (Video, [id])
        | Regex @"(?<=list=)(P[\w\-]+)" [ id ] -> Some (StandardPlaylist, [id])
        | Regex @"(?<=list=)(O[\w\-]+)" [ id ] -> Some (ReleasePlaylist, [id])
        | Regex @"((?:www\.)?youtube\.com\/(?:channel\/|c\/|user\/|@)(?:[\w\-]+))" [ id ] -> Some (Channel, [id])
        | _ -> None

    let private mediaTypeWithDownloadUrls (typeWithIds : (MediaType * string list) option) =
        let videoUrlBase = "https://www.youtube.com/watch?v="
        let playlistUrlBase = "https://www.youtube.com/playlist?list="
        let channelUrlBase = "https://"

        let fullUrl urlBase id = urlBase + id
        let videoUrl = fullUrl videoUrlBase
        let playlistUrl = fullUrl playlistUrlBase
        let channelUrl = fullUrl channelUrlBase

        match typeWithIds with
        | Some (_, ids) when ids.Length = 0 -> None // TODO: Error
        | Some (Video, ids) -> Some (Video, [videoUrl ids.Head])
        | Some (PlaylistVideo, ids) when ids.Length = 2
            -> Some (PlaylistVideo, [videoUrl ids.Head; playlistUrl ids.[1]])
        | Some (StandardPlaylist, ids) -> Some (StandardPlaylist , [playlistUrl ids.Head])
        | Some (ReleasePlaylist, ids) -> Some (ReleasePlaylist, [playlistUrl ids.Head])
        | Some (Channel, ids) -> Some (Channel, [channelUrl ids.Head])
        | _ -> None // TODO: Or, `Error`?

    let generateDownloadUrls rawUrl =
        mediaTypeWithIds rawUrl
        |> mediaTypeWithDownloadUrls
