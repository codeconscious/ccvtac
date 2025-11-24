namespace CCVTAC.Console.PostProcessing

open System
open System.Text

module YouTubeMetadataExtensionMethods =
    type VideoMetadata with

        /// Returns a string summarizing video uploader information.
        member private this.UploaderSummary() : string =
            let uploaderLinkOrIdOrEmpty =
                if not (String.IsNullOrWhiteSpace this.UploaderUrl) then this.UploaderUrl
                elif not (String.IsNullOrWhiteSpace this.UploaderId) then this.UploaderId
                else String.Empty

            let suffix = if not (String.IsNullOrWhiteSpace uploaderLinkOrIdOrEmpty) then sprintf " (%s)" uploaderLinkOrIdOrEmpty else String.Empty
            this.Uploader + suffix

        /// Returns a formatted MM/DD/YYYY version of the upload date (e.g., "08/27/2023")
        /// from the plain YYYYMMDD version (e.g., "20230827").
        member private this.FormattedUploadDate() : string =
            // Assumes UploadDate has at least 8 characters (YYYYMMDD)
            let y = if String.IsNullOrEmpty this.UploadDate then "" else this.UploadDate.[0..3]
            let m = if this.UploadDate.Length >= 6 then this.UploadDate.[4..5] else ""
            let d = if this.UploadDate.Length >= 8 then this.UploadDate.[6..7] else ""
            sprintf "%s/%s/%s" m d y

        /// Returns a formatted comment using data parsed from the JSON file.
        member this.GenerateComment(maybeCollectionData: CollectionMetadata option) : string =
            let sb = StringBuilder()
            sb.AppendLine("CCVTAC SOURCE DATA:") |> ignore
            sb.AppendLine(sprintf "■ Downloaded: %O" DateTime.Now) |> ignore
            sb.AppendLine(sprintf "■ URL: %s" this.WebpageUrl) |> ignore
            sb.AppendLine(sprintf "■ Title: %s" this.Fulltitle) |> ignore
            sb.AppendLine(sprintf "■ Uploader: %s" (this.UploaderSummary())) |> ignore

            if not (String.IsNullOrWhiteSpace this.Creator) && this.Creator <> this.Uploader then
                sb.AppendLine(sprintf "■ Creator: %s" this.Creator) |> ignore

            if not (String.IsNullOrWhiteSpace this.Artist) then
                sb.AppendLine(sprintf "■ Artist: %s" this.Artist) |> ignore

            if not (String.IsNullOrWhiteSpace this.Album) then
                sb.AppendLine(sprintf "■ Album: %s" this.Album) |> ignore

            if not (String.IsNullOrWhiteSpace this.Title) && this.Title <> this.Fulltitle then
                sb.AppendLine(sprintf "■ Track Title: %s" this.Title) |> ignore

            sb.AppendLine(sprintf "■ Uploaded: %s" (this.FormattedUploadDate())) |> ignore

            let description =
                if String.IsNullOrWhiteSpace this.Description then "None." else this.Description

            sb.AppendLine(sprintf "■ Video description: %s" description) |> ignore

            match maybeCollectionData with
            | Some collectionData ->
                sb.AppendLine() |> ignore
                sb.AppendLine(sprintf "■ Playlist name: %s" collectionData.Title) |> ignore
                sb.AppendLine(sprintf "■ Playlist URL: %s" collectionData.WebpageUrl) |> ignore
                match this.PlaylistIndex with
                | NullV -> ()
                | NonNullV index -> if index > 0u then sb.AppendLine(sprintf "■ Playlist index: %d" index) |> ignore
                sb.AppendLine(sprintf "■ Playlist description: %s" (if String.IsNullOrWhiteSpace collectionData.Description then String.Empty else collectionData.Description)) |> ignore
            | None -> ()

            sb.ToString()
