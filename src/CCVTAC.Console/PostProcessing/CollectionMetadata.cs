using System.Text.Json.Serialization;

namespace CCVTAC.Console.PostProcessing;

/// <summary>
/// Represents the JSON file of a YouTube playlist or channel. (Both contain
/// the fields that I use, so this is a combined object for now. This might
/// be changed in the future, though.)
/// </summary>
public readonly record struct CollectionMetadata(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("availability")] string Availability,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("tags")] IReadOnlyList<object> Tags,
    [property: JsonPropertyName("modified_date")] string ModifiedDate,
    [property: JsonPropertyName("view_count")] int? ViewCount,
    [property: JsonPropertyName("playlist_count")] int? PlaylistCount,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("channel_id")] string ChannelId,
    [property: JsonPropertyName("uploader_id")] string UploaderId,
    [property: JsonPropertyName("uploader")] string Uploader,
    [property: JsonPropertyName("channel_url")] string ChannelUrl,
    [property: JsonPropertyName("uploader_url")] string UploaderUrl,
    [property: JsonPropertyName("_type")] string Type,
    [property: JsonPropertyName("webpage_url")] string WebpageUrl,
    [property: JsonPropertyName("webpage_url_basename")] string WebpageUrlBasename,
    [property: JsonPropertyName("webpage_url_domain")] string WebpageUrlDomain,
    [property: JsonPropertyName("epoch")] int? Epoch
);
