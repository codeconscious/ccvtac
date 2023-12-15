namespace CCVTAC.FSharp

module Say =
    let hello name =
        printfn "Hello %s" name

module YouTube =
    type MediaId = { Id: string }
    type SingleId = { MediaId: MediaId }
    type IdPair = { PrimaryId: MediaId; SupplementaryId: MediaId }

    type MediaType =
        | PlaylistVideo of resourcePair: IdPair
        | Video of resourceId: SingleId
        | StandardPlaylist of resourceId: SingleId
        | ReleasePlaylist of resourceId: SingleId
        | Channel of resourceId: SingleId

    type DownloadType =
        | MediaType of MediaType
        | Metadata
