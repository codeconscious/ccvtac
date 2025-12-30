namespace CCVTAC.Main.PostProcessing

open System.Text.Json.Serialization

type CollectionMetadata =
    { [<property: JsonPropertyName("id")>] Id: string
      [<property: JsonPropertyName("title")>] Title: string
      [<property: JsonPropertyName("availability")>] Availability: string
      [<property: JsonPropertyName("description")>] Description: string
      [<property: JsonPropertyName("tags")>] Tags: string list
      [<property: JsonPropertyName("modified_date")>] ModifiedDate: string
      [<property: JsonPropertyName("view_count")>] ViewCount: int option
      [<property: JsonPropertyName("playlist_count")>] PlaylistCount: int option
      [<property: JsonPropertyName("channel")>] Channel: string
      [<property: JsonPropertyName("channel_id")>] ChannelId: string
      [<property: JsonPropertyName("uploader_id")>] UploaderId: string
      [<property: JsonPropertyName("uploader")>] Uploader: string
      [<property: JsonPropertyName("channel_url")>] ChannelUrl: string
      [<property: JsonPropertyName("uploader_url")>] UploaderUrl: string
      [<property: JsonPropertyName("_type")>] Type: string
      [<property: JsonPropertyName("webpage_url")>] WebpageUrl: string
      [<property: JsonPropertyName("webpage_url_basename")>] WebpageUrlBasename: string
      [<property: JsonPropertyName("webpage_url_domain")>] WebpageUrlDomain: string
      [<property: JsonPropertyName("epoch")>] Epoch: int option }
