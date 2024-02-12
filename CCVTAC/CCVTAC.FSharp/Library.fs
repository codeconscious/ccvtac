namespace CCVTAC.FSharp

module Say =
    let hello name =
        printfn "Hello %s" name

module YouTube =
    open System.Text.RegularExpressions

    // type VideoId = VideoId of string
    // type PlaylistId = PlaylistId of string
    // type ChannelId = ChannelId of string
    type Url = Url of string
    // type Id = Id of string
    type MediaType = | Video | PlaylistVideo | StandardPlaylist | ReleasePlaylist | Channel | Unknown
    type IdPair = { VideoId: string; PlaylistId: string }

    let parseUrl (s:string) (rgx, mediaType) =
        let matches = System.Text.RegularExpressions.Regex.Matches(s, rgx)
        if matches.Count > 1
        then Some (matches |> Seq.cast |> Seq.filter (fun (regMatch:Match) -> regMatch.Success) |> Seq.map (fun (regMatch:Match) -> regMatch.Value))
        else None

    //  let unwrap (Media media) = media

    type DownloadType =
        | MediaType of MediaType
        | Metadata

    let urlPattern (media:MediaType) (url:string) =
        match media with
        | Video str -> sprintf "https://www.youtube.com/watch?v=%s" str
        | PlaylistVideo -> "https://www.youtube.com/watch?v=%s"
        | StandardPlaylist -> "https://www.youtube.com/playlist?list=%s"
        | ReleasePlaylist -> "https://www.youtube.com/playlist?list=%s"
        | Channel -> "https://"
        | _ -> ""

    let fullUrl (media: Media) =
        urlBase media + match media with
                        | Video id -> id
                        | PlaylistVideo (videoId, playlistId) -> videoId + playlistId // TODO: Fix
                        | StandardPlaylist id -> id
                        | ReleasePlaylist id -> id
                        | Channel id -> id
                        | Unknown -> failwith "Not Implemented"

    // Source: https://stackoverflow.com/questions/53818476/f-match-many-regex
    // Reference: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns
    // This is an "active recognizer."
    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None

    // let getMedia (url:string) =
    //     match url with
    //     | Regex "^([\w-]{11})$" [ id ] -> Video({VideoId = id})
    //     | Regex "(?<=v=|v\\=)([\w-]{11})" [ id ] -> Video({VideoId = id})
    //     | Regex "(?<=youtu\.be/)(.{11})" [ id ] -> Video({VideoId = id})
    //     | Regex "(?<=v=|v\=)([\w-]{11})(?:&list=([\w_-]+))" [ id ; parentId] -> PlaylistVideo({VideoId = id; ParentId = parentId})
    //     | Regex "(?<=list=)(P[\w\-]+)" [ id ] -> StandardPlaylist({VideoId = id})
    //     | Regex "(?<=list=)(O[\w\-]+)" [ id ] -> ReleasePlaylist({VideoId = id})
    //     | Regex "((?:www\.)?youtube\.com\/(?:channel\/|c\/|user\/|@)(?:[\w\-]+))" [ id ] -> Channel({VideoId = id})
    //     | _ -> Unknown
