namespace CCVTAC.Console.PostProcessing

open System
open System.Collections.Generic
open System.Text
open System.Text.Json.Serialization
open CCVTAC.Console

[<Struct>]
type VideoMetadata =
    { [<property: JsonPropertyName("id")>] Id: string
      [<property: JsonPropertyName("title")>] Title: string
      [<property: JsonPropertyName("thumbnail")>] Thumbnail: string
      [<property: JsonPropertyName("description")>] Description: string
      [<property: JsonPropertyName("channel_id")>] ChannelId: string
      [<property: JsonPropertyName("channel_url")>] ChannelUrl: string
      [<property: JsonPropertyName("duration")>] Duration: Nullable<int>
      [<property: JsonPropertyName("view_count")>] ViewCount: Nullable<int>
      [<property: JsonPropertyName("age_limit")>] AgeLimit: Nullable<int>
      [<property: JsonPropertyName("webpage_url")>] WebpageUrl: string
      [<property: JsonPropertyName("categories")>] Categories: IReadOnlyList<string>
      [<property: JsonPropertyName("tags")>] Tags: IReadOnlyList<string>
      [<property: JsonPropertyName("playable_in_embed")>] PlayableInEmbed: Nullable<bool>
      [<property: JsonPropertyName("live_status")>] LiveStatus: string
      [<property: JsonPropertyName("release_timestamp")>] ReleaseTimestamp: Nullable<int>
      [<property: JsonPropertyName("_format_sort_fields")>] FormatSortFields: IReadOnlyList<string>
      [<property: JsonPropertyName("album")>] Album: string
      [<property: JsonPropertyName("artist")>] Artist: string
      [<property: JsonPropertyName("track")>] Track: string
      [<property: JsonPropertyName("comment_count")>] CommentCount: Nullable<int>
      [<property: JsonPropertyName("like_count")>] LikeCount: Nullable<int>
      [<property: JsonPropertyName("channel")>] Channel: string
      [<property: JsonPropertyName("channel_follower_count")>] ChannelFollowerCount: Nullable<int>
      [<property: JsonPropertyName("channel_is_verified")>] ChannelIsVerified: Nullable<bool>
      [<property: JsonPropertyName("uploader")>] Uploader: string
      [<property: JsonPropertyName("uploader_id")>] UploaderId: string
      [<property: JsonPropertyName("uploader_url")>] UploaderUrl: string
      [<property: JsonPropertyName("upload_date")>] UploadDate: string
      [<property: JsonPropertyName("creator")>] Creator: string
      [<property: JsonPropertyName("alt_title")>] AltTitle: string
      [<property: JsonPropertyName("availability")>] Availability: string
      [<property: JsonPropertyName("webpage_url_basename")>] WebpageUrlBasename: string
      [<property: JsonPropertyName("webpage_url_domain")>] WebpageUrlDomain: string
      [<property: JsonPropertyName("extractor")>] Extractor: string
      [<property: JsonPropertyName("extractor_key")>] ExtractorKey: string
      [<property: JsonPropertyName("playlist_count")>] PlaylistCount: Nullable<int>
      [<property: JsonPropertyName("playlist")>] Playlist: string
      [<property: JsonPropertyName("playlist_id")>] PlaylistId: string
      [<property: JsonPropertyName("playlist_title")>] PlaylistTitle: string
      [<property: JsonPropertyName("n_entries")>] NEntries: Nullable<int>
      [<property: JsonPropertyName("playlist_index")>] PlaylistIndex: Nullable<uint32>
      [<property: JsonPropertyName("display_id")>] DisplayId: string
      [<property: JsonPropertyName("fulltitle")>] Fulltitle: string
      [<property: JsonPropertyName("duration_string")>] DurationString: string
      [<property: JsonPropertyName("release_date")>] ReleaseDate: string
      [<property: JsonPropertyName("release_year")>] ReleaseYear: Nullable<uint32>
      [<property: JsonPropertyName("is_live")>] IsLive: Nullable<bool>
      [<property: JsonPropertyName("was_live")>] WasLive: Nullable<bool>
      [<property: JsonPropertyName("epoch")>] Epoch: Nullable<int>
      [<property: JsonPropertyName("asr")>] Asr: Nullable<int>
      [<property: JsonPropertyName("filesize")>] Filesize: Nullable<int>
      [<property: JsonPropertyName("format_id")>] FormatId: string
      [<property: JsonPropertyName("format_note")>] FormatNote: string
      [<property: JsonPropertyName("source_preference")>] SourcePreference: Nullable<int>
      [<property: JsonPropertyName("audio_channels")>] AudioChannels: Nullable<int>
      [<property: JsonPropertyName("quality")>] Quality: Nullable<double>
      [<property: JsonPropertyName("has_drm")>] HasDrm: Nullable<bool>
      [<property: JsonPropertyName("tbr")>] Tbr: Nullable<double>
      [<property: JsonPropertyName("url")>] Url: string
      [<property: JsonPropertyName("language_preference")>] LanguagePreference: Nullable<int>
      [<property: JsonPropertyName("ext")>] Ext: string
      [<property: JsonPropertyName("vcodec")>] Vcodec: string
      [<property: JsonPropertyName("acodec")>] Acodec: string
      [<property: JsonPropertyName("container")>] Container: string
      [<property: JsonPropertyName("protocol")>] Protocol: string
      [<property: JsonPropertyName("resolution")>] Resolution: string
      [<property: JsonPropertyName("audio_ext")>] AudioExt: string
      [<property: JsonPropertyName("video_ext")>] VideoExt: string
      [<property: JsonPropertyName("vbr")>] Vbr: Nullable<int>
      [<property: JsonPropertyName("abr")>] Abr: Nullable<double>
      [<property: JsonPropertyName("format")>] Format: string
      [<property: JsonPropertyName("_type")>] Type: string }

    /// Returns a string summarizing video uploader information.
    member private this.UploaderSummary() : string =
        let uploaderLinkOrIdOrEmpty =
            if hasText this.UploaderUrl then this.UploaderUrl
            elif hasText this.UploaderId then this.UploaderId
            else String.Empty

        let suffix = if hasText uploaderLinkOrIdOrEmpty then $" (%s{uploaderLinkOrIdOrEmpty})" else String.Empty
        this.Uploader + suffix

    /// Returns a formatted MM/DD/YYYY version of the upload date (e.g., "08/27/2023")
    /// from the plain YYYYMMDD version (e.g., "20230827").
    member private this.FormattedUploadDate() : string =
        // Assumes UploadDate has at least 8 characters (YYYYMMDD)
        let y = if String.IsNullOrEmpty this.UploadDate then "" else this.UploadDate[0..3]
        let m = if this.UploadDate.Length >= 6 then this.UploadDate[4..5] else ""
        let d = if this.UploadDate.Length >= 8 then this.UploadDate[6..7] else ""
        sprintf "%s/%s/%s" m d y

    /// Returns a formatted comment using data parsed from the JSON file.
    member this.GenerateComment(maybeCollectionData: CollectionMetadata option) : string =
        let sb = StringBuilder()
        sb.AppendLine("CCVTAC SOURCE DATA:") |> ignore
        sb.AppendLine(sprintf "■ Downloaded: %O" DateTime.Now) |> ignore
        sb.AppendLine(sprintf "■ URL: %s" this.WebpageUrl) |> ignore
        sb.AppendLine(sprintf "■ Title: %s" this.Fulltitle) |> ignore
        sb.AppendLine(sprintf "■ Uploader: %s" (this.UploaderSummary())) |> ignore

        if hasText this.Creator && this.Creator <> this.Uploader then
            sb.AppendLine(sprintf "■ Creator: %s" this.Creator) |> ignore

        if hasText this.Artist then
            sb.AppendLine(sprintf "■ Artist: %s" this.Artist) |> ignore

        if hasText this.Album then
            sb.AppendLine(sprintf "■ Album: %s" this.Album) |> ignore

        if hasText this.Title && this.Title <> this.Fulltitle then
            sb.AppendLine(sprintf "■ Track Title: %s" this.Title) |> ignore

        sb.AppendLine(sprintf "■ Uploaded: %s" (this.FormattedUploadDate())) |> ignore

        let description =
            if hasNoText this.Description then "None." else this.Description

        sb.AppendLine(sprintf "■ Video description: %s" description) |> ignore

        match maybeCollectionData with
        | Some collectionData ->
            sb.AppendLine() |> ignore
            sb.AppendLine(sprintf "■ Playlist name: %s" collectionData.Title) |> ignore
            sb.AppendLine(sprintf "■ Playlist URL: %s" collectionData.WebpageUrl) |> ignore
            match this.PlaylistIndex with
            | NonNullV (index: uint32) -> if index > 0u then sb.AppendLine(sprintf "■ Playlist index: %d" index) |> ignore
            | NullV -> ()
            sb.AppendLine(sprintf "■ Playlist description: %s" (if hasNoText collectionData.Description then String.Empty else collectionData.Description)) |> ignore
        | None -> ()

        sb.ToString()
