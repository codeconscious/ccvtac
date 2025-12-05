namespace CCVTAC.Console.PostProcessing

open System
open System.Text
open CCVTAC.Console

module MetadataUtilities =

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

    let generateComment (v: VideoMetadata) (c: CollectionMetadata option) : string =
        let sb = StringBuilder()
        sb.AppendLine("CCVTAC SOURCE DATA:") |> ignore
        sb.AppendLine $"■ Downloaded: {DateTime.Now}" |> ignore
        sb.AppendLine $"■ URL: %s{v.WebpageUrl}" |> ignore
        sb.AppendLine $"■ Title: %s{v.Fulltitle}" |> ignore
        sb.AppendLine $"■ Uploader: %s{uploaderSummary v}" |> ignore

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

        let description = String.textOrFallback "None." v.Description
        sb.AppendLine $"■ Video description: %s{description}" |> ignore

        match c with
        | Some c' ->
            sb.AppendLine() |> ignore
            sb.AppendLine $"■ Playlist name: %s{c'.Title}" |> ignore
            sb.AppendLine $"■ Playlist URL: %s{c'.WebpageUrl}" |> ignore
            match v.PlaylistIndex with
                | Some index -> if index > 0u then sb.AppendLine $"■ Playlist index: %d{index}" |> ignore
                | None -> ()
            sb.AppendLine($"■ Playlist description: %s{String.textOrEmpty c'.Description}") |> ignore
        | None -> ()

        sb.ToString()
