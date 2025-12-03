namespace CCVTAC.Console.PostProcessing

open System
open System.Text
open CCVTAC.Console

module MetadataUtilities =

    /// Returns a string summarizing video uploader information.
    let private uploaderSummary (v: VideoMetadata) : string =
        let suffix =
            match List.tryFind String.hasText [v.UploaderUrl; v.UploaderId] with
            | Some x -> $" (%s{x})"
            | None -> String.Empty
        v.Uploader + suffix

    let private formattedUploadDate (dateText: string) : string =
        let y = dateText[0..3]
        let m = dateText[4..5]
        let d = dateText[6..7]
        sprintf "%s/%s/%s" m d y

    /// Returns a formatted comment using data parsed from the JSON file.
    let generateComment (v: VideoMetadata) (maybeCollectionData: CollectionMetadata option) : string =
        let sb = StringBuilder()
        sb.AppendLine("CCVTAC SOURCE DATA:") |> ignore
        sb.AppendLine(sprintf "■ Downloaded: %O" DateTime.Now) |> ignore
        sb.AppendLine(sprintf "■ URL: %s" v.WebpageUrl) |> ignore
        sb.AppendLine(sprintf "■ Title: %s" v.Fulltitle) |> ignore
        sb.AppendLine(sprintf "■ Uploader: %s" (uploaderSummary v)) |> ignore

        if String.hasText v.Creator && v.Creator <> v.Uploader then
            sb.AppendLine $"■ Creator: %s{v.Creator}" |> ignore

        if String.hasText v.Artist then
            sb.AppendLine $"■ Artist: %s{v.Artist}" |> ignore

        if String.hasText v.Album then
            sb.AppendLine $"■ Album: %s{v.Album}" |> ignore

        if String.hasText v.Title && v.Title <> v.Fulltitle then
            sb.AppendLine $"■ Track Title: %s{v.Title}" |> ignore

        if v.UploadDate.Length = 8 then
            sb.AppendLine $"■ Uploaded: %s{formattedUploadDate v.UploadDate}" |> ignore

        let description =
            if String.hasNoText v.Description then "None." else v.Description

        sb.AppendLine(sprintf "■ Video description: %s" description) |> ignore

        match maybeCollectionData with
        | Some collectionData ->
            sb.AppendLine() |> ignore
            sb.AppendLine(sprintf "■ Playlist name: %s" collectionData.Title) |> ignore
            sb.AppendLine(sprintf "■ Playlist URL: %s" collectionData.WebpageUrl) |> ignore
            match v.PlaylistIndex with
            | NullV -> ()
            | NonNullV index -> if index > 0u then sb.AppendLine(sprintf "■ Playlist index: %d" index) |> ignore
            sb.AppendLine(sprintf "■ Playlist description: %s" (if String.hasNoText collectionData.Description then String.Empty else collectionData.Description)) |> ignore
        | None -> ()

        sb.ToString()
