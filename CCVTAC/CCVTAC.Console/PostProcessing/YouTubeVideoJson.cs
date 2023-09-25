using System.Text.Json.Serialization;

namespace CCVTAC.Console.PostProcessing;

/// <summary>
/// Represents the data containing the JSON file downloaded alongside the video.
/// </summary>
/// <remarks>I recommend using https://json2csharp.com/ for generating this.</remarks>
public record struct YouTubeVideoJson
{
    public record struct Root(
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
        [property: JsonPropertyName("automatic_captions")] AutomaticCaptions AutomaticCaptions,
        [property: JsonPropertyName("subtitles")] Subtitles Subtitles,
        [property: JsonPropertyName("heatmap")] IReadOnlyList<Heatmap> Heatmap,
        [property: JsonPropertyName("comment_count")] int? CommentCount,
        [property: JsonPropertyName("like_count")] int? LikeCount,
        [property: JsonPropertyName("channel")] string Channel,
        [property: JsonPropertyName("channel_follower_count")] int? ChannelFollowerCount,
        [property: JsonPropertyName("channel_is_verified")] bool? ChannelIsVerified,
        [property: JsonPropertyName("uploader")] string Uploader,
        [property: JsonPropertyName("uploader_id")] string UploaderId,
        [property: JsonPropertyName("uploader_url")] string UploaderUrl,
        [property: JsonPropertyName("upload_date")] string UploadDate,
        [property: JsonPropertyName("availability")] string Availability,
        [property: JsonPropertyName("webpage_url_basename")] string WebpageUrlBasename,
        [property: JsonPropertyName("webpage_url_domain")] string WebpageUrlDomain,
        [property: JsonPropertyName("extractor")] string Extractor,
        [property: JsonPropertyName("extractor_key")] string ExtractorKey,
        [property: JsonPropertyName("display_id")] string DisplayId,
        [property: JsonPropertyName("fulltitle")] string Fulltitle,
        [property: JsonPropertyName("duration_string")] string DurationString,
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

    public record struct AutomaticCaptions(
        [property: JsonPropertyName("en")] IReadOnlyList<En> En
    );

    public record struct DownloaderOptions(
        [property: JsonPropertyName("http_chunk_size")] int? HttpChunkSize
    );

    public record struct En(
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("ext")] string Ext,
        [property: JsonPropertyName("protocol")] string Protocol
    );

    public record struct EnNP72PuUl7o(
        [property: JsonPropertyName("ext")] string Ext,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("name")] string Name
    );

    public record struct FormatInfo(
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
        [property: JsonPropertyName("has_drm")] object HasDrm,
        [property: JsonPropertyName("source_preference")] int? SourcePreference,
        [property: JsonPropertyName("asr")] int? Asr,
        [property: JsonPropertyName("filesize")] int? Filesize,
        [property: JsonPropertyName("audio_channels")] int? AudioChannels,
        [property: JsonPropertyName("tbr")] double? Tbr,
        [property: JsonPropertyName("language_preference")] int? LanguagePreference,
        [property: JsonPropertyName("container")] string Container,
        [property: JsonPropertyName("downloader_options")] DownloaderOptions DownloaderOptions,
        [property: JsonPropertyName("preference")] int? Preference,
        [property: JsonPropertyName("dynamic_range")] string DynamicRange,
        [property: JsonPropertyName("filesize_approx")] int? FilesizeApprox
    );

    public record struct Fragment(
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("duration")] double? Duration
    );

    public record struct Heatmap(
        [property: JsonPropertyName("start_time")] double? Start_time,
        [property: JsonPropertyName("end_time")] double? End_time,
        [property: JsonPropertyName("value")] double? value
    );

    public record struct HttpHeaders(
        [property: JsonPropertyName("User-Agent")] string UserAgent,
        [property: JsonPropertyName("Accept")] string Accept,
        [property: JsonPropertyName("Accept-Language")] string AcceptLanguage,
        [property: JsonPropertyName("Sec-Fetch-Mode")] string SecFetchMode
    );

    public record struct Subtitles(
        [property: JsonPropertyName("en-nP7-2PuUl7o")] IReadOnlyList<EnNP72PuUl7o> EnNP72PuUl7o
    );

    public record struct Thumbnail(
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("preference")] int? Preference,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("height")] int? Height,
        [property: JsonPropertyName("width")] int? Width,
        [property: JsonPropertyName("resolution")] string Resolution
    );

    public record struct VersionInfo(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("release_git_head")] string ReleaseGitHead,
        [property: JsonPropertyName("repository")] string Repository
    );


}
