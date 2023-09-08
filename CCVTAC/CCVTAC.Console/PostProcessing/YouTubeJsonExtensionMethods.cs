namespace CCVTAC.Console.PostProcessing;

public static class YouTubeJsonExtensionMethods
{
    /// <summary>
    /// Returns a string summarizing video uploader information.
    /// </summary>
    public static string UploaderSummary(this YouTubeVideoJson.Root data)
    {
        var uploaderLinkOrId = string.IsNullOrWhiteSpace(data.uploader_url)
            ? data.uploader_id
            : data.uploader_url;

        return $"{data.uploader} ({uploaderLinkOrId})";
    }

    /// <summary>
    /// Returns a formatted MM/DD/YYYY version of the upload date (e.g., "08/27/2023") from the
    /// plain YYYYMMDD version (e.g., "20230827") within the parsed JSON file data.
    /// </summary>
    public static string FormattedUploadDate(this YouTubeVideoJson.Root data) =>
        $"{data.upload_date[4..6]}/{data.upload_date[6..8]}/{data.upload_date[0..4]}";

    /// <summary>
    /// Returns a formatted comment using data parsed from the JSON file.
    /// </summary>
    public static string GenerateComment(this YouTubeVideoJson.Root data, YouTubePlaylistJson.Root? playlistJson)
    {
        System.Text.StringBuilder sb = new();
        sb.AppendLine("SOURCE DATA:");
        sb.AppendLine($"• Downloaded: {DateTime.Now} using CCVTAC");
        sb.AppendLine($"• Service: {data.extractor_key}");
        sb.AppendLine($"• URL: {data.webpage_url}");
        sb.AppendLine($"• Title: {data.fulltitle}");
        sb.AppendLine($"• Uploader: {data.UploaderSummary()}");
        sb.AppendLine($"• Uploaded: {data.FormattedUploadDate()}");
        if (playlistJson is not null)
        {
            sb.AppendLine($"• Playlist name: {playlistJson.Title}");
            sb.AppendLine($"• Playlist URL: {playlistJson.WebpageUrl}");
            if (!string.IsNullOrWhiteSpace(playlistJson.Description))
            {
                sb.AppendLine($"• Playlist description: {playlistJson.Description}");
            }
        }
        sb.AppendLine($"• Description: {data.description})");
        return sb.ToString();
    }
}
