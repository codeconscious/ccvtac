namespace CCVTAC.Console.PostProcessing;

public static class YouTubeJsonExtensionMethods
{
    /// <summary>
    /// Returns a string summarizing video uploader information.
    /// </summary>
    public static string UploaderSummary(this VideoMetadata videoData)
    {
        string uploaderLinkOrId = !string.IsNullOrWhiteSpace(videoData.UploaderUrl)
            ? videoData.UploaderUrl
            : !string.IsNullOrWhiteSpace(videoData.UploaderId)
                ? videoData.UploaderId
                : string.Empty;

        return videoData.Uploader +
               (string.IsNullOrWhiteSpace(uploaderLinkOrId) ? string.Empty : $" ({uploaderLinkOrId})");
    }

    /// <summary>
    /// Returns a formatted MM/DD/YYYY version of the upload date (e.g., "08/27/2023") from the
    /// plain YYYYMMDD version (e.g., "20230827") within the parsed JSON file data.
    /// </summary>
    public static string FormattedUploadDate(this VideoMetadata videoData) =>
        $"{videoData.UploadDate[4..6]}/{videoData.UploadDate[6..8]}/{videoData.UploadDate[0..4]}";

    /// <summary>
    /// Returns a formatted comment using data parsed from the JSON file.
    /// </summary>
    public static string GenerateComment(this VideoMetadata videoData, CollectionMetadata? maybeCollectionData)
    {
        System.Text.StringBuilder sb = new();
        sb.AppendLine("CCVTAC SOURCE DATA:");
        sb.AppendLine($"• Downloaded: {DateTime.Now}");
        // sb.AppendLine($"• Service: {videoData.ExtractorKey}");
        sb.AppendLine($"• URL: {videoData.WebpageUrl}");
        sb.AppendLine($"• Title: {videoData.Fulltitle}");
        sb.AppendLine($"• Uploader: {videoData.UploaderSummary()}");
        if (videoData.Creator != videoData.Uploader && !string.IsNullOrWhiteSpace(videoData.Creator))
        {
            sb.AppendLine($"• Creator: {videoData.Creator}");
        }
        sb.AppendLine($"• Uploaded: {videoData.FormattedUploadDate()}");
        if (maybeCollectionData is CollectionMetadata collectionData)
        {
            sb.AppendLine($"• Playlist name: {collectionData.Title}");
            sb.AppendLine($"• Playlist URL: {collectionData.WebpageUrl}");
            sb.AppendLine($"• Playlist index: {videoData.PlaylistIndex}");
            if (!string.IsNullOrWhiteSpace(collectionData.Description))
            {
                sb.AppendLine($"• Playlist description: {collectionData.Description}");
            }
        }
        var description = string.IsNullOrWhiteSpace(videoData.Description) ? "None." : videoData.Description;
        sb.AppendLine($"• Video description: {description}");
        return sb.ToString();
    }
}
