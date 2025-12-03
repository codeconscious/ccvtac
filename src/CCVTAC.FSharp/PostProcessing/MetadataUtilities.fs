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

    /// Returns a formatted MM/DD/YYYY version of the upload date (e.g., "08/27/2023")
    /// from the plain YYYYMMDD version (e.g., "20230827").
    let private formattedUploadDate (v: VideoMetadata) : string =
        // Assumes UploadDate has at least 8 characters (YYYYMMDD)
        let y = if String.IsNullOrEmpty v.UploadDate then String.Empty else v.UploadDate.[0..3]
        let m = if v.UploadDate.Length >= 6 then v.UploadDate.[4..5] else String.Empty
        let d = if v.UploadDate.Length >= 8 then v.UploadDate.[6..7] else String.Empty
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

        sb.AppendLine $"■ Uploaded: %s{formattedUploadDate v}" |> ignore

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
