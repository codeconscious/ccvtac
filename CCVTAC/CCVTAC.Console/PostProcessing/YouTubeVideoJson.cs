using System.Text.Json.Serialization;

namespace CCVTAC.Console.PostProcessing;

public record struct YouTubeVideoJson
{
    public record struct Root(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("formats")] IReadOnlyList<FormatData> Formats,
        [property: JsonPropertyName("thumbnails")] IReadOnlyList<Thumbnail> Thumbnails,
        [property: JsonPropertyName("thumbnail")] string Thumbnail,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("channel_id")] string Channel_id,
        [property: JsonPropertyName("channel_url")] string Channel_url,
        [property: JsonPropertyName("duration")] int? Duration,
        [property: JsonPropertyName("view_count")] int? View_count,
        [property: JsonPropertyName("age_limit")] int? Age_limit,
        [property: JsonPropertyName("webpage_url")] string Webpage_url,
        [property: JsonPropertyName("categories")] IReadOnlyList<string> Categories,
        [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags,
        [property: JsonPropertyName("playable_in_embed")] bool? Playable_in_embed,
        [property: JsonPropertyName("live_status")] string Live_status,
        [property: JsonPropertyName("automatic_captions")] AutomaticCaptions Automatic_captions,
        [property: JsonPropertyName("subtitles")] Subtitles Subtitles,
        [property: JsonPropertyName("heatmap")] IReadOnlyList<Heatmap> Heatmap,
        [property: JsonPropertyName("like_count")] int? Like_count,
        [property: JsonPropertyName("channel")] string Channel,
        [property: JsonPropertyName("channel_follower_count")] int? Channel_follower_count,
        [property: JsonPropertyName("channel_is_verified")] bool? Channel_is_verified,
        [property: JsonPropertyName("uploader")] string Uploader,
        [property: JsonPropertyName("uploader_id")] string Uploader_id,
        [property: JsonPropertyName("uploader_url")] string Uploader_url,
        [property: JsonPropertyName("upload_date")] string Upload_date,
        [property: JsonPropertyName("availability")] string Availability,
        [property: JsonPropertyName("webpage_url_basename")] string Webpage_url_basename,
        [property: JsonPropertyName("webpage_url_domain")] string Webpage_url_domain,
        [property: JsonPropertyName("extractor")] string Extractor,
        [property: JsonPropertyName("extractor_key")] string Extractor_key,
        [property: JsonPropertyName("display_id")] string Display_id,
        [property: JsonPropertyName("fulltitle")] string Fulltitle,
        [property: JsonPropertyName("duration_string")] string Duration_string,
        [property: JsonPropertyName("is_live")] bool? Is_live,
        [property: JsonPropertyName("was_live")] bool? Was_live,
        [property: JsonPropertyName("epoch")] int? Epoch,
        [property: JsonPropertyName("asr")] int? Asr,
        [property: JsonPropertyName("filesize")] int? Filesize,
        [property: JsonPropertyName("format_id")] string Format_id,
        [property: JsonPropertyName("format_note")] string Format_note,
        [property: JsonPropertyName("source_preference")] int? Source_preference,
        [property: JsonPropertyName("audio_channels")] int? Audio_channels,
        [property: JsonPropertyName("quality")] double? Quality,
        [property: JsonPropertyName("has_drm")] bool? Has_drm,
        [property: JsonPropertyName("tbr")] double? Tbr,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("language_preference")] int? Language_preference,
        [property: JsonPropertyName("ext")] string Ext,
        [property: JsonPropertyName("vcodec")] string Vcodec,
        [property: JsonPropertyName("acodec")] string Acodec,
        [property: JsonPropertyName("container")] string Container,
        [property: JsonPropertyName("downloader_options")] DownloaderOptions Downloader_options,
        [property: JsonPropertyName("protocol")] string Protocol,
        [property: JsonPropertyName("resolution")] string Resolution,
        [property: JsonPropertyName("http_headers")] HttpHeaders Http_headers,
        [property: JsonPropertyName("audio_ext")] string Audio_ext,
        [property: JsonPropertyName("video_ext")] string Video_ext,
        [property: JsonPropertyName("vbr")] int? Vbr,
        [property: JsonPropertyName("abr")] double? Abr,
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("_type")] string _type,
        [property: JsonPropertyName("_version")] VersionInfo _version
    );

    public record struct AutomaticCaptions(

    );

    public record struct DownloaderOptions(
        [property: JsonPropertyName("http_chunk_size")] int? http_chunk_size
    );

    public record struct FormatData(
        [property: JsonPropertyName("format_id")] string Format_id,
        [property: JsonPropertyName("format_note")] string Format_note,
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
        [property: JsonPropertyName("aspect_ratio")] double? Aspect_ratio,
        [property: JsonPropertyName("http_headers")] HttpHeaders Http_headers,
        [property: JsonPropertyName("audio_ext")] string Audio_ext,
        [property: JsonPropertyName("video_ext")] string Video_ext,
        [property: JsonPropertyName("vbr")] double? Vbr,
        [property: JsonPropertyName("abr")] double? Abr,
        [property: JsonPropertyName("format")] string Format,
        [property: JsonPropertyName("manifest_url")] string Manifest_url,
        [property: JsonPropertyName("quality")] double? Quality,
        [property: JsonPropertyName("has_drm")] bool? Has_drm,
        [property: JsonPropertyName("source_preference")] int? Source_preference,
        [property: JsonPropertyName("asr")] int? Asr,
        [property: JsonPropertyName("filesize")] int? Filesize,
        [property: JsonPropertyName("audio_channels")] int? Audio_channels,
        [property: JsonPropertyName("tbr")] double? Tbr,
        [property: JsonPropertyName("language_preference")] int? Language_preference,
        [property: JsonPropertyName("container")] string Container,
        [property: JsonPropertyName("downloader_options")] DownloaderOptions Downloader_options,
        [property: JsonPropertyName("preference")] int? Preference,
        [property: JsonPropertyName("dynamic_range")] string Dynamic_range,
        [property: JsonPropertyName("filesize_approx")] int? filesize_approx
    );

    public record struct Fragment(
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("duration")] double? duration
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
        // TODO: Look into deleting.
    );

    public record struct Thumbnail(
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("preference")] int? Preference,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("height")] int? Height,
        [property: JsonPropertyName("width")] int? Width,
        [property: JsonPropertyName("resolution")] string resolution
    );

    public record struct VersionInfo(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("release_git_head")] string Release_git_head,
        [property: JsonPropertyName("repository")] string repository
    );
}
