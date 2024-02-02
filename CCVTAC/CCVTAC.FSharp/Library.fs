namespace CCVTAC.FSharp

module Say =
    let hello name =
        printfn "Hello %s" name

module YouTube =
    open System.Text.RegularExpressions

    // type Id = { VideoId: string }
    type Id = Id of string
    let CreateId (s:string) =
        if System.Text.RegularExpressions.Regex.IsMatch(s,@".{11}")
            then Some (Id s)
            else None

    type IdPair = { VideoId: string; ParentId: string }

    type Media =
        | Video of Id
        | PlaylistVideo of IdPair
        | StandardPlaylist of Id
        | ReleasePlaylist of Id
        | Channel of Id
        | Unknown

    type DownloadType =
        | MediaType of Media
        | Metadata

    let urlBase (media:Media) =
        match media with
        | Video _ -> "https://www.youtube.com/watch?v="
        | PlaylistVideo _ -> "https://www.youtube.com/watch?v="
        | StandardPlaylist _ -> "https://www.youtube.com/playlist?list="
        | ReleasePlaylist _ -> "https://www.youtube.com/playlist?list="
        | Channel _ -> "https://"
        | _ -> ""

    let fullUrl (media:Media) =
        urlBase media + match media with
                        | Video data -> data.VideoId
                        | PlaylistVideo data -> data.VideoId
                        | StandardPlaylist data -> data.VideoId
                        | ReleasePlaylist data -> data.VideoId
                        | Channel data -> data.VideoId
                        | Unknown -> failwith "Not Implemented"

    // Source: https://stackoverflow.com/questions/53818476/f-match-many-regex
    // Reference: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns
    // This is an "active recognizer."
    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None

    let getMedia (url:string) =
        match url with
        | Regex "^([\w-]{11})$" [ id ] -> Video({VideoId = id})
        | Regex "(?<=v=|v\\=)([\w-]{11})" [ id ] -> Video({VideoId = id})
        | Regex "(?<=youtu\.be/)(.{11})" [ id ] -> Video({VideoId = id})
        | Regex "(?<=v=|v\=)([\w-]{11})(?:&list=([\w_-]+))" [ id ; parentId] -> PlaylistVideo({VideoId = id; ParentId = parentId})
        | Regex "(?<=list=)(P[\w\-]+)" [ id ] -> StandardPlaylist({VideoId = id})
        | Regex "(?<=list=)(O[\w\-]+)" [ id ] -> ReleasePlaylist({VideoId = id})
        | Regex "((?:www\.)?youtube\.com\/(?:channel\/|c\/|user\/|@)(?:[\w\-]+))" [ id ] -> Channel({VideoId = id})
        | _ -> Unknown
