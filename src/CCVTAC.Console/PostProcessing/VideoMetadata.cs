using System.Text.Json.Serialization;

namespace CCVTAC.Console.PostProcessing;

/// <summary>
/// Represents the data containing the JSON file downloaded alongside the video.
/// </summary>
public readonly record struct VideoMetadata(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("formats")] IReadOnlyList<FormatInfo> Formats,
    [property: JsonPropertyName("thumbnails")] IReadOnlyList<Thumbnail> Thumbnails,
    [property: JsonPropertyName("thumbnail")] string Thumbnail,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("channel_id")] string ChannelId,
    [property: JsonPropertyName("channel_url")] string ChannelUrl,
    [property: JsonPropertyName("duration")] int? Duration,
    [property: JsonPropertyName("view_count")] int? ViewCount,
    [property: JsonPropertyName("age_limit")] int? AgeLimit,
    [property: JsonPropertyName("webpage_url")] string WebpageUrl,
    [property: JsonPropertyName("categories")] IReadOnlyList<string> Categories,
    [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags,
    [property: JsonPropertyName("playable_in_embed")] bool? PlayableInEmbed,
    [property: JsonPropertyName("live_status")] string LiveStatus,
    [property: JsonPropertyName("release_timestamp")] int? ReleaseTimestamp,
    [property: JsonPropertyName("_format_sort_fields")] IReadOnlyList<string> FormatSortFields,
    [property: JsonPropertyName("automatic_captions")] AutomaticCaptions AutomaticCaptions,
    [property: JsonPropertyName("subtitles")] Subtitles Subtitles,
    [property: JsonPropertyName("album")] string Album,
    [property: JsonPropertyName("artist")] string Artist,
    [property: JsonPropertyName("track")] string Track,
    [property: JsonPropertyName("comment_count")] int? CommentCount,
    [property: JsonPropertyName("chapters")] IReadOnlyList<Chapter> Chapters,
    [property: JsonPropertyName("like_count")] int? LikeCount,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("channel_follower_count")] int? ChannelFollowerCount,
    [property: JsonPropertyName("channel_is_verified")] bool? ChannelIsVerified,
    [property: JsonPropertyName("uploader")] string Uploader,
    [property: JsonPropertyName("uploader_id")] string UploaderId,
    [property: JsonPropertyName("uploader_url")] string UploaderUrl,
    [property: JsonPropertyName("upload_date")] string UploadDate,
    [property: JsonPropertyName("creator")] string Creator,
    [property: JsonPropertyName("alt_title")] string AltTitle,
    [property: JsonPropertyName("availability")] string Availability,
    [property: JsonPropertyName("webpage_url_basename")] string WebpageUrlBasename,
    [property: JsonPropertyName("webpage_url_domain")] string WebpageUrlDomain,
    [property: JsonPropertyName("extractor")] string Extractor,
    [property: JsonPropertyName("extractor_key")] string ExtractorKey,
    [property: JsonPropertyName("playlist_count")] int? PlaylistCount,
    [property: JsonPropertyName("playlist")] string Playlist,
    [property: JsonPropertyName("playlist_id")] string PlaylistId,
    [property: JsonPropertyName("playlist_title")] string PlaylistTitle,
    [property: JsonPropertyName("n_entries")] int? NEntries,
    [property: JsonPropertyName("playlist_index")] uint? PlaylistIndex,
    [property: JsonPropertyName("display_id")] string DisplayId,
    [property: JsonPropertyName("fulltitle")] string Fulltitle,
    [property: JsonPropertyName("duration_string")] string DurationString,
    [property: JsonPropertyName("release_date")] string ReleaseDate,
    [property: JsonPropertyName("release_year")] uint? ReleaseYear,
    [property: JsonPropertyName("is_live")] bool? IsLive,
    [property: JsonPropertyName("was_live")] bool? WasLive,
    [property: JsonPropertyName("epoch")] int? Epoch,
    [property: JsonPropertyName("asr")] int? Asr,
    [property: JsonPropertyName("filesize")] int? Filesize,
    [property: JsonPropertyName("format_id")] string FormatId,
    [property: JsonPropertyName("format_note")] string FormatNote,
    [property: JsonPropertyName("source_preference")] int? SourcePreference,
    [property: JsonPropertyName("audio_channels")] int? AudioChannels,
    [property: JsonPropertyName("quality")] double? Quality,
    [property: JsonPropertyName("has_drm")] bool? HasDrm,
    [property: JsonPropertyName("tbr")] double? Tbr,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("language_preference")] int? LanguagePreference,
    [property: JsonPropertyName("ext")] string Ext,
    [property: JsonPropertyName("vcodec")] string Vcodec,
    [property: JsonPropertyName("acodec")] string Acodec,
    [property: JsonPropertyName("container")] string Container,
    [property: JsonPropertyName("downloader_options")] DownloaderOptions DownloaderOptions,
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("resolution")] string Resolution,
    [property: JsonPropertyName("http_headers")] HttpHeaders HttpHeaders,
    [property: JsonPropertyName("audio_ext")] string AudioExt,
    [property: JsonPropertyName("video_ext")] string VideoExt,
    [property: JsonPropertyName("vbr")] int? Vbr,
    [property: JsonPropertyName("abr")] double? Abr,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("_type")] string Type,
    [property: JsonPropertyName("_version")] VersionInfo Version
);

public readonly record struct AutomaticCaptions(
    [property: JsonPropertyName("en")] IReadOnlyList<En> En
);

public readonly record struct DownloaderOptions(
    [property: JsonPropertyName("http_chunk_size")] int? HttpChunkSize
);

public readonly record struct En(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("ext")] string Ext,
    [property: JsonPropertyName("protocol")] string Protocol
);

public readonly record struct EnNP72PuUl7o(
    [property: JsonPropertyName("ext")] string Ext,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("name")] string Name
);

public readonly record struct FormatInfo(
    [property: JsonPropertyName("format_id")] string FormatId,
    [property: JsonPropertyName("format_note")] string FormatNote,
    [property: JsonPropertyName("ext")] string Ext,
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("acodec")] string Acodec,
    [property: JsonPropertyName("vcodec")] string Vcodec,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("fps")] double? Fps,
    [property: JsonPropertyName("rows")] int? Rows,
    [property: JsonPropertyName("columns")] int? Columns,
    [property: JsonPropertyName("fragments")] IReadOnlyList<Fragment> Fragments,
    [property: JsonPropertyName("resolution")] string Resolution,
    [property: JsonPropertyName("aspect_ratio")] double? AspectRatio,
    [property: JsonPropertyName("http_headers")] HttpHeaders HttpHeaders,
    [property: JsonPropertyName("audio_ext")] string AudioExt,
    [property: JsonPropertyName("video_ext")] string VideoExt,
    [property: JsonPropertyName("vbr")] double? Vbr,
    [property: JsonPropertyName("abr")] double? Abr,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("manifest_url")] string ManifestUrl,
    [property: JsonPropertyName("quality")] double? Quality,
    [property: JsonPropertyName("has_drm")] bool? HasDrm,
    [property: JsonPropertyName("source_preference")] int? SourcePreference,
    [property: JsonPropertyName("asr")] int? Asr,
    [property: JsonPropertyName("filesize")] long? Filesize,
    [property: JsonPropertyName("audio_channels")] int? AudioChannels,
    [property: JsonPropertyName("tbr")] double? Tbr,
    [property: JsonPropertyName("language_preference")] int? LanguagePreference,
    [property: JsonPropertyName("container")] string Container,
    [property: JsonPropertyName("downloader_options")] DownloaderOptions DownloaderOptions,
    [property: JsonPropertyName("preference")] int? Preference,
    [property: JsonPropertyName("dynamic_range")] string DynamicRange,
    [property: JsonPropertyName("filesize_approx")] long? FilesizeApprox
);

public readonly record struct Fragment(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("duration")] double? Duration
);

public readonly record struct HttpHeaders(
    [property: JsonPropertyName("User-Agent")] string UserAgent,
    [property: JsonPropertyName("Accept")] string Accept,
    [property: JsonPropertyName("Accept-Language")] string AcceptLanguage,
    [property: JsonPropertyName("Sec-Fetch-Mode")] string SecFetchMode
);

public readonly record struct Subtitles(
    [property: JsonPropertyName("en-nP7-2PuUl7o")] IReadOnlyList<EnNP72PuUl7o> EnNP72PuUl7o,
    [property: JsonPropertyName("live_chat")] IReadOnlyList<LiveChat> LiveChat
);

public record LiveChat(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("video_id")] string VideoId,
    [property: JsonPropertyName("ext")] string Ext,
    [property: JsonPropertyName("protocol")] string Protocol
);

public readonly record struct Thumbnail(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("preference")] int? Preference,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("height")] int? Height,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("resolution")] string Resolution
);

public readonly record struct VersionInfo(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("release_git_head")] string ReleaseGitHead,
    [property: JsonPropertyName("repository")] string Repository
);

public record Chapter(
    [property: JsonPropertyName("start_time")] double? StartTime,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("end_time")] double? EndTime
);
