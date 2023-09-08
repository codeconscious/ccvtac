namespace CCVTAC.Console.PostProcessing;

public static class YouTubeJsonExtensionMethods
{
    /// <summary>
    /// Returns a string summarizing video uploader information.
    /// </summary>
    public static string UploaderSummary(this YouTubeVideoJson.Root data)
    {
        var uploaderLinkOrId = string.IsNullOrWhiteSpace(data.Uploader_url)
            ? data.Uploader_id
            : data.Uploader_url;

        return $"{data.Uploader} ({uploaderLinkOrId})";
    }

    /// <summary>
    /// Returns a formatted MM/DD/YYYY version of the upload date (e.g., "08/27/2023") from the
    /// plain YYYYMMDD version (e.g., "20230827") within the parsed JSON file data.
    /// </summary>
    public static string FormattedUploadDate(this YouTubeVideoJson.Root data) =>
        $"{data.Upload_date[4..6]}/{data.Upload_date[6..8]}/{data.Upload_date[0..4]}";

    /// <summary>
    /// Returns a formatted comment using data parsed from the JSON file.
    /// </summary>
    public static string GenerateComment(this YouTubeVideoJson.Root data, YouTubePlaylistJson.Root? playlistJson)
    {
        System.Text.StringBuilder sb = new();
        sb.AppendLine("SOURCE DATA:");
        sb.AppendLine($"• Downloaded: {DateTime.Now} using CCVTAC");
        sb.AppendLine($"• Service: {data.Extractor_key}");
        sb.AppendLine($"• URL: {data.Webpage_url}");
        sb.AppendLine($"• Title: {data.Fulltitle}");
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
        sb.AppendLine($"• Description: {data.Description})");
        return sb.ToString();
    }
}
