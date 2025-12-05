namespace CCVTAC.Console.PostProcessing

open System.Collections.Generic
open System.Text.Json.Serialization

type VideoMetadata = {
    [<property: JsonPropertyName("id")>] Id: string
    [<property: JsonPropertyName("title")>] Title: string
    [<property: JsonPropertyName("thumbnail")>] Thumbnail: string
    [<property: JsonPropertyName("description")>] Description: string
    [<property: JsonPropertyName("channel_id")>] ChannelId: string
    [<property: JsonPropertyName("channel_url")>] ChannelUrl: string
    [<property: JsonPropertyName("duration")>] Duration: int option
    [<property: JsonPropertyName("view_count")>] ViewCount: int option
    [<property: JsonPropertyName("age_limit")>] AgeLimit: int option
    [<property: JsonPropertyName("webpage_url")>] WebpageUrl: string
    [<property: JsonPropertyName("categories")>] Categories: string list
    [<property: JsonPropertyName("tags")>] Tags: string list
    [<property: JsonPropertyName("playable_in_embed")>] PlayableInEmbed: bool option
    [<property: JsonPropertyName("live_status")>] LiveStatus: string
    [<property: JsonPropertyName("release_timestamp")>] ReleaseTimestamp: int option
    [<property: JsonPropertyName("_format_sort_fields")>] FormatSortFields: string list
    [<property: JsonPropertyName("album")>] Album: string
    [<property: JsonPropertyName("artist")>] Artist: string
    [<property: JsonPropertyName("track")>] Track: string
    [<property: JsonPropertyName("comment_count")>] CommentCount: int option
    [<property: JsonPropertyName("like_count")>] LikeCount: int option
    [<property: JsonPropertyName("channel")>] Channel: string
    [<property: JsonPropertyName("channel_follower_count")>] ChannelFollowerCount: int option
    [<property: JsonPropertyName("channel_is_verified")>] ChannelIsVerified: bool option
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
    [<property: JsonPropertyName("playlist_count")>] PlaylistCount: int option
    [<property: JsonPropertyName("playlist")>] Playlist: string
    [<property: JsonPropertyName("playlist_id")>] PlaylistId: string
    [<property: JsonPropertyName("playlist_title")>] PlaylistTitle: string
    [<property: JsonPropertyName("n_entries")>] NEntries: int option
    [<property: JsonPropertyName("playlist_index")>] PlaylistIndex: uint32 option
    [<property: JsonPropertyName("display_id")>] DisplayId: string
    [<property: JsonPropertyName("fulltitle")>] Fulltitle: string
    [<property: JsonPropertyName("duration_string")>] DurationString: string
    [<property: JsonPropertyName("release_date")>] ReleaseDate: string
    [<property: JsonPropertyName("release_year")>] ReleaseYear: uint32 option
    [<property: JsonPropertyName("is_live")>] IsLive: bool option
    [<property: JsonPropertyName("was_live")>] WasLive: bool option
    [<property: JsonPropertyName("epoch")>] Epoch: int option
    [<property: JsonPropertyName("asr")>] Asr: int option
    [<property: JsonPropertyName("filesize")>] Filesize: int option
    [<property: JsonPropertyName("format_id")>] FormatId: string
    [<property: JsonPropertyName("format_note")>] FormatNote: string
    [<property: JsonPropertyName("source_preference")>] SourcePreference: int option
    [<property: JsonPropertyName("audio_channels")>] AudioChannels: int option
    [<property: JsonPropertyName("quality")>] Quality: double option
    [<property: JsonPropertyName("has_drm")>] HasDrm: bool option
    [<property: JsonPropertyName("tbr")>] Tbr: double option
    [<property: JsonPropertyName("url")>] Url: string
    [<property: JsonPropertyName("language_preference")>] LanguagePreference: int option
    [<property: JsonPropertyName("ext")>] Ext: string
    [<property: JsonPropertyName("vcodec")>] Vcodec: string
    [<property: JsonPropertyName("acodec")>] Acodec: string
    [<property: JsonPropertyName("container")>] Container: string
    [<property: JsonPropertyName("protocol")>] Protocol: string
    [<property: JsonPropertyName("resolution")>] Resolution: string
    [<property: JsonPropertyName("audio_ext")>] AudioExt: string
    [<property: JsonPropertyName("video_ext")>] VideoExt: string
    [<property: JsonPropertyName("vbr")>] Vbr: int option
    [<property: JsonPropertyName("abr")>] Abr: double option
    [<property: JsonPropertyName("format")>] Format: string
    [<property: JsonPropertyName("_type")>] Type: string }
