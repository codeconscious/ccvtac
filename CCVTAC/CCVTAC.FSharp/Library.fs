namespace CCVTAC.FSharp

module YouTube =
    open System.Text.RegularExpressions

    type Url = Url of string

    // type VideoId = VideoId of string
    // type PlaylistId = PlaylistId of string
    // type ChannelId = ChannelId of string

    // type MediaId =
    //     | VideoId
    //     | PlaylistId
    //     | ChannelId

    type MediaType =
        | Video
        | PlaylistVideo
        | StandardPlaylist
        | ReleasePlaylist
        | Channel
        | Unknown

    // type DownloadType =
    //     | MediaType of MediaType
    //     | Metadata

    // Source: https://stackoverflow.com/questions/53818476/f-match-many-regex
    // Reference: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns
    // Reference: https://jason-down.com/2017/01/24/f-pattern-matching-part-2-active-patterns/
    // This is an "active recognizer."
    let (|Regex|_|) input pattern =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None

    let mediaTypeWithUrls (realUrl:Url) =
        let (Url url) = realUrl
        match url with
        | Regex "^([\w-]{11})$" [ id ] -> (Video, [id])
        | Regex "(?<=v=|v\\=)([\w-]{11})" [ id ] -> (Video, [id])
        | Regex "(?<=youtu\.be/)(.{11})" [ id ] -> (Video, [id])
        | Regex "(?<=v=|v\=)([\w-]{11})(?:&list=([\w_-]+))" [ id ; parentId]
            -> (PlaylistVideo, [id; parentId])
        | Regex "(?<=list=)(P[\w\-]+)" [ id ] -> (StandardPlaylist, [id])
        | Regex "(?<=list=)(O[\w\-]+)" [ id ] -> (ReleasePlaylist, [id])
        | Regex "((?:www\.)?youtube\.com\/(?:channel\/|c\/|user\/|@)(?:[\w\-]+))" [ id ] -> (Channel, [id])
        | _ -> (Unknown, [])
        // | failwith "The URL didn't match any registered URL pattern..."

    // let parseUrl (url1:Url) (rgx, mediaType) =
    //     let (Url url2) = url1
    //     let matches = System.Text.RegularExpressions.Regex.Matches(url2, rgx)
    //     if matches.Count = 0
    //     then None
    //     else Some (matches
    //                |> Seq.cast
    //                |> Seq.filter (fun (regMatch:Match) -> regMatch.Success)
    //                |> Seq.map (fun (regMatch:Match) -> regMatch.Value))

    let createCleanUrls (mediaType, ids:string list) =
        // let id = ids.[0]
        match mediaType with
        | Video when ids.Length = 1
            -> Some ([sprintf "https://www.youtube.com/watch?v=%s" ids.[0]])
        | PlaylistVideo when ids.Length = 2
            -> Some ([sprintf "https://www.youtube.com/watch?v=%s" ids.[0];
                      sprintf "https://www.youtube.com/playlist?list=%s" ids.[1]])
        | StandardPlaylist | ReleasePlaylist
            -> Some ([sprintf "https://www.youtube.com/playlist?list=%s" ids.[0]])
        | Channel
            -> Some ([sprintf "https://%s" ids.[0]])
        | _ -> None
        // | failwith "An invalid count of ids was passed for the media type."

    // let fullUrl (media: Media) =
    //     urlBase media + match media with
    //                     | Video id -> id
    //                     | PlaylistVideo (videoId, playlistId) -> videoId + playlistId // TODO: Fix
    //                     | StandardPlaylist id -> id
    //                     | ReleasePlaylist id -> id
    //                     | Channel id -> id
    //                     | Unknown -> failwith "Not Implemented"

    let generateUrlsFromInput rawUrl =
        let inputUrl = Url rawUrl
        let urls = mediaTypeWithUrls inputUrl |> createCleanUrls
        urls
