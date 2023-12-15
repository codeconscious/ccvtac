namespace CCVTAC.FSharp

module Say =
    let hello name =
        printfn "Hello %s" name

module YouTube =
    type ResourceId = string

    type PlaylistVideo = { VideoId: ResourceId; PlaylistId: ResourceId }
    type Video = { Id: ResourceId }
    type StandardPlaylist = { Id: ResourceId }
    type ReleasePlaylist = { Id: ResourceId } // Entries on "Releases" tabs on YouTube
    type Channel = { Id: ResourceId }

    type MediaType =
        | PlaylistVideo
        | Video
        | StandardPlaylist
        | ReleasePlaylist
        | Channel

    type DownloadType =
        | MediaType of MediaType
        | Metadata
