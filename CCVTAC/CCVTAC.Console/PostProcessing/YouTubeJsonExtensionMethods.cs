namespace CCVTAC.Console.PostProcessing;

public static class YouTubeJsonExtensionMethods
{
    /// <summary>
    /// Returns a string summarizing video uploader information.
    /// </summary>
    public static string UploaderSummary(this YouTubeVideoJson.Root data)
    {
        var uploaderLinkOrId = string.IsNullOrWhiteSpace(data.UploaderUrl)
            ? data.UploaderId
            : data.UploaderUrl;

        return $"{data.Uploader} ({uploaderLinkOrId})";
    }

    /// <summary>
    /// Returns a formatted MM/DD/YYYY version of the upload date (e.g., "08/27/2023") from the
    /// plain YYYYMMDD version (e.g., "20230827") within the parsed JSON file data.
    /// </summary>
    public static string FormattedUploadDate(this YouTubeVideoJson.Root data) =>
        $"{data.UploadDate[4..6]}/{data.UploadDate[6..8]}/{data.UploadDate[0..4]}";

    /// <summary>
    /// Returns a formatted comment using data parsed from the JSON file.
    /// </summary>
    public static string GenerateComment(
        this YouTubeVideoJson.Root data,
        YouTubeCollectionJson.Root? collectionJson)
    {
        System.Text.StringBuilder sb = new();
        sb.AppendLine("SOURCE DATA:");
        sb.AppendLine($"• Downloaded: {DateTime.Now} using CCVTAC");
        sb.AppendLine($"• Service: {data.ExtractorKey}");
        sb.AppendLine($"• URL: {data.WebpageUrl}");
        sb.AppendLine($"• Title: {data.Fulltitle}");
        sb.AppendLine($"• Uploader: {data.UploaderSummary()}");
        sb.AppendLine($"• Uploaded: {data.FormattedUploadDate()}");
        if (collectionJson is not null)
        {
            sb.AppendLine($"• Playlist name: {collectionJson.Title}");
            sb.AppendLine($"• Playlist URL: {collectionJson.WebpageUrl}");
            if (!string.IsNullOrWhiteSpace(collectionJson.Description))
            {
                sb.AppendLine($"• Playlist description: {collectionJson.Description}");
            }
        }
        sb.AppendLine($"• Description: {data.Description})");
        return sb.ToString();
    }
}
