namespace CCVTAC.FSharp

module public YouTube =
    open System.Text.RegularExpressions

    type private Url = Url of string // 本当に要るかどうか、決まっていない。

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

    // Original source credit: https://stackoverflow.com/questions/53818476/f-match-many-regex
    // Reference: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns
    // Reference: https://jason-down.com/2017/01/24/f-pattern-matching-part-2-active-patterns/
    // This is an "active recognizer." (Also, this breaks if the parameter order is switched!)
    let private (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        match m with
            | m when m.Success -> Some(List.tail [ for g in m.Groups -> g.Value ])
            | _ -> None

    let private mediaTypeWithUrls (url:Url) = // TODO: `private`にしたい。
        let (Url textUrl) = url
        match textUrl with
            | Regex @"(?<=v=|v\=)([\w-]{11})(?:&list=([\w_-]+))" [ id ; playlistId ]
                -> (PlaylistVideo, [id; playlistId])
            | Regex @"^([\w-]{11})$" [ id ] -> (Video, [id])
            | Regex @"(?<=v=|v\\=)([\w-]{11})" [ id ] -> (Video, [id])
            | Regex @"(?<=youtu\.be/)(.{11})" [ id ] -> (Video, [id])
            | Regex @"(?<=list=)(P[\w\-]+)" [ id ] -> (StandardPlaylist, [id])
            | Regex @"(?<=list=)(O[\w\-]+)" [ id ] -> (ReleasePlaylist, [id])
            | Regex @"((?:www\.)?youtube\.com\/(?:channel\/|c\/|user\/|@)(?:[\w\-]+))" [ id ] -> (Channel, [id])
            | _ -> (Unknown, [])

    let private createCleanUrls (mediaType, ids:string list) =
        match mediaType with
        | Video when ids.Length = 1
            -> Some (mediaType, [sprintf "https://www.youtube.com/watch?v=%s" ids.[0]])
        | PlaylistVideo when ids.Length = 2
            -> Some (mediaType, [sprintf "https://www.youtube.com/watch?v=%s" ids.[0];
                                 sprintf "https://www.youtube.com/playlist?list=%s" ids.[1]])
        | StandardPlaylist | ReleasePlaylist when ids.Length = 1
            -> Some (mediaType, [sprintf "https://www.youtube.com/playlist?list=%s" ids.[0]])
        | Channel when ids.Length = 1
            -> Some (mediaType, [sprintf "https://%s" ids.[0]])
        | _ -> None

    let generateDownloadUrls rawUrl =
        mediaTypeWithUrls (Url rawUrl) |> createCleanUrls
