namespace CCVTAC.FSharp

module public Downloading =
    open System.Text.RegularExpressions

    type MediaType =
        | Video of id : string
        | PlaylistVideo of videoId : string * playlistId : string
        | StandardPlaylist of id : string
        | ReleasePlaylist of id : string
        | Channel of id : string

    // type Download =
    //     | Media of MediaType
    //     | Metadata

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
        | _ -> Error "Unable to determine the media type of the URL."

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

    // module public Args =
    //     let generateArgs settings download customArgs =
    //         let baseArgs download =
    //             let writeJson = "--write-info-json"
    //             let trimFileNames = "--trim-filenames 250"

    //             match download with
    //             | Media -> String.concat " " ($"--extract-audio -f {settings.AudioFormat}" +
    //                                           "--write-thumbnail --convert-thumbnails jpg" + // For album art
    //                                           writeJson + // Contains metadata
    //                                           trimFileNames +
    //                                           "--retries 3")
    //             | Metadata -> $"--flat-playlist {writeJson} {trimFileNames}"

    //         let verbosityArgs args =
    //             settings.
