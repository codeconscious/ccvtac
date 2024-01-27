namespace CCVTAC.FSharp

module Say =
    let hello name =
        printfn "Hello %s" name

module YouTube =
    type MediaId = { VideoId: string }
    type MediaIdPair = { VideoId: string; ParentId: string }

    type Media =
        | Video of MediaId
        | PlaylistVideo of MediaIdPair
        | StandardPlaylist of MediaIdPair
        | ReleasePlaylist of MediaIdPair
        | Channel of MediaIdPair

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

    let fullUrl (media:Media) =
        urlBase media + match media with
                        | Video data -> data.VideoId
                        | PlaylistVideo data -> data.VideoId
                        | StandardPlaylist data -> data.VideoId
                        | ReleasePlaylist data -> data.VideoId
                        | Channel data -> data.VideoId
