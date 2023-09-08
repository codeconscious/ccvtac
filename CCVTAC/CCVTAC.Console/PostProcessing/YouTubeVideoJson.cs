using System.Text.Json.Serialization;

namespace CCVTAC.Console.PostProcessing;

public record struct YouTubeVideoJson
{
    public record struct Root(
        [property: JsonPropertyName("id")] string id,
        [property: JsonPropertyName("title")] string title,
        [property: JsonPropertyName("formats")] IReadOnlyList<Format> formats,
        [property: JsonPropertyName("thumbnails")] IReadOnlyList<Thumbnail> thumbnails,
        [property: JsonPropertyName("thumbnail")] string thumbnail,
        [property: JsonPropertyName("description")] string description,
        [property: JsonPropertyName("channel_id")] string channel_id,
        [property: JsonPropertyName("channel_url")] string channel_url,
        [property: JsonPropertyName("duration")] int? duration,
        [property: JsonPropertyName("view_count")] int? view_count,
        [property: JsonPropertyName("age_limit")] int? age_limit,
        [property: JsonPropertyName("webpage_url")] string webpage_url,
        [property: JsonPropertyName("categories")] IReadOnlyList<string> categories,
        [property: JsonPropertyName("tags")] IReadOnlyList<string> tags,
        [property: JsonPropertyName("playable_in_embed")] bool? playable_in_embed,
        [property: JsonPropertyName("live_status")] string live_status,
        [property: JsonPropertyName("automatic_captions")] AutomaticCaptions automatic_captions,
        [property: JsonPropertyName("subtitles")] Subtitles subtitles,
        [property: JsonPropertyName("heatmap")] IReadOnlyList<Heatmap> heatmap,
        [property: JsonPropertyName("like_count")] int? like_count,
        [property: JsonPropertyName("channel")] string channel,
        [property: JsonPropertyName("channel_follower_count")] int? channel_follower_count,
        [property: JsonPropertyName("channel_is_verified")] bool? channel_is_verified,
        [property: JsonPropertyName("uploader")] string uploader,
        [property: JsonPropertyName("uploader_id")] string uploader_id,
        [property: JsonPropertyName("uploader_url")] string uploader_url,
        [property: JsonPropertyName("upload_date")] string upload_date,
        [property: JsonPropertyName("availability")] string availability,
        [property: JsonPropertyName("webpage_url_basename")] string webpage_url_basename,
        [property: JsonPropertyName("webpage_url_domain")] string webpage_url_domain,
        [property: JsonPropertyName("extractor")] string extractor,
        [property: JsonPropertyName("extractor_key")] string extractor_key,
        [property: JsonPropertyName("display_id")] string display_id,
        [property: JsonPropertyName("fulltitle")] string fulltitle,
        [property: JsonPropertyName("duration_string")] string duration_string,
        [property: JsonPropertyName("is_live")] bool? is_live,
        [property: JsonPropertyName("was_live")] bool? was_live,
        [property: JsonPropertyName("epoch")] int? epoch,
        [property: JsonPropertyName("asr")] int? asr,
        [property: JsonPropertyName("filesize")] int? filesize,
        [property: JsonPropertyName("format_id")] string format_id,
        [property: JsonPropertyName("format_note")] string format_note,
        [property: JsonPropertyName("source_preference")] int? source_preference,
        [property: JsonPropertyName("audio_channels")] int? audio_channels,
        [property: JsonPropertyName("quality")] double? quality,
        [property: JsonPropertyName("has_drm")] bool? has_drm,
        [property: JsonPropertyName("tbr")] double? tbr,
        [property: JsonPropertyName("url")] string url,
        [property: JsonPropertyName("language_preference")] int? language_preference,
        [property: JsonPropertyName("ext")] string ext,
        [property: JsonPropertyName("vcodec")] string vcodec,
        [property: JsonPropertyName("acodec")] string acodec,
        [property: JsonPropertyName("container")] string container,
        [property: JsonPropertyName("downloader_options")] DownloaderOptions downloader_options,
        [property: JsonPropertyName("protocol")] string protocol,
        [property: JsonPropertyName("resolution")] string resolution,
        [property: JsonPropertyName("http_headers")] HttpHeaders http_headers,
        [property: JsonPropertyName("audio_ext")] string audio_ext,
        [property: JsonPropertyName("video_ext")] string video_ext,
        [property: JsonPropertyName("vbr")] int? vbr,
        [property: JsonPropertyName("abr")] double? abr,
        [property: JsonPropertyName("format")] string format,
        [property: JsonPropertyName("_type")] string _type,
        [property: JsonPropertyName("_version")] Version _version
    );

    public record struct AutomaticCaptions(

    );

    public record struct DownloaderOptions(
        [property: JsonPropertyName("http_chunk_size")] int? http_chunk_size
    );

    public record struct Format(
        [property: JsonPropertyName("format_id")] string format_id,
        [property: JsonPropertyName("format_note")] string format_note,
        [property: JsonPropertyName("ext")] string ext,
        [property: JsonPropertyName("protocol")] string protocol,
        [property: JsonPropertyName("acodec")] string acodec,
        [property: JsonPropertyName("vcodec")] string vcodec,
        [property: JsonPropertyName("url")] string url,
        [property: JsonPropertyName("width")] int? width,
        [property: JsonPropertyName("height")] int? height,
        [property: JsonPropertyName("fps")] double? fps,
        [property: JsonPropertyName("rows")] int? rows,
        [property: JsonPropertyName("columns")] int? columns,
        [property: JsonPropertyName("fragments")] IReadOnlyList<Fragment> fragments,
        [property: JsonPropertyName("resolution")] string resolution,
        [property: JsonPropertyName("aspect_ratio")] double? aspect_ratio,
        [property: JsonPropertyName("http_headers")] HttpHeaders http_headers,
        [property: JsonPropertyName("audio_ext")] string audio_ext,
        [property: JsonPropertyName("video_ext")] string video_ext,
        [property: JsonPropertyName("vbr")] double? vbr,
        [property: JsonPropertyName("abr")] double? abr,
        [property: JsonPropertyName("format")] string format,
        [property: JsonPropertyName("manifest_url")] string manifest_url,
        [property: JsonPropertyName("quality")] double? quality,
        [property: JsonPropertyName("has_drm")] bool? has_drm,
        [property: JsonPropertyName("source_preference")] int? source_preference,
        [property: JsonPropertyName("asr")] int? asr,
        [property: JsonPropertyName("filesize")] int? filesize,
        [property: JsonPropertyName("audio_channels")] int? audio_channels,
        [property: JsonPropertyName("tbr")] double? tbr,
        [property: JsonPropertyName("language_preference")] int? language_preference,
        [property: JsonPropertyName("container")] string container,
        [property: JsonPropertyName("downloader_options")] DownloaderOptions downloader_options,
        [property: JsonPropertyName("preference")] int? preference,
        [property: JsonPropertyName("dynamic_range")] string dynamic_range,
        [property: JsonPropertyName("filesize_approx")] int? filesize_approx
    );

    public record struct Fragment(
        [property: JsonPropertyName("url")] string url,
        [property: JsonPropertyName("duration")] double? duration
    );

    public record struct Heatmap(
        [property: JsonPropertyName("start_time")] double? start_time,
        [property: JsonPropertyName("end_time")] double? end_time,
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
        [property: JsonPropertyName("url")] string url,
        [property: JsonPropertyName("preference")] int? preference,
        [property: JsonPropertyName("id")] string id,
        [property: JsonPropertyName("height")] int? height,
        [property: JsonPropertyName("width")] int? width,
        [property: JsonPropertyName("resolution")] string resolution
    );

    public record struct Version(
        [property: JsonPropertyName("version")] string version,
        [property: JsonPropertyName("release_git_head")] string release_git_head,
        [property: JsonPropertyName("repository")] string repository
    );
}
