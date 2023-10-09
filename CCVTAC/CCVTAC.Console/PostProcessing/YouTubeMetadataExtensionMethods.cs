namespace CCVTAC.Console.PostProcessing;

public static class YouTubeMetadataExtensionMethods
{
    /// <summary>
    /// Returns a string summarizing video uploader information.
    /// </summary>
    public static string UploaderSummary(this VideoMetadata videoData)
    {
        string uploaderLinkOrIdOrEmpty = !string.IsNullOrWhiteSpace(videoData.UploaderUrl)
            ? videoData.UploaderUrl
            : !string.IsNullOrWhiteSpace(videoData.UploaderId)
                ? videoData.UploaderId
                : string.Empty;

        return videoData.Uploader +
               (string.IsNullOrWhiteSpace(uploaderLinkOrIdOrEmpty)
                    ? string.Empty :
                    $" ({uploaderLinkOrIdOrEmpty})");
    }

    /// <summary>
    /// Returns a formatted MM/DD/YYYY version of the upload date (e.g., "08/27/2023") from the
    /// plain YYYYMMDD version (e.g., "20230827") within the parsed JSON file data.
    /// </summary>
    public static string FormattedUploadDate(this VideoMetadata videoData)
    {
        return $"{videoData.UploadDate[4..6]}/{videoData.UploadDate[6..8]}/{videoData.UploadDate[0..4]}";
    }

    /// <summary>
    /// Returns a formatted comment using data parsed from the JSON file.
    /// </summary>
    public static string GenerateComment(this VideoMetadata videoData, CollectionMetadata? maybeCollectionData)
    {
        System.Text.StringBuilder sb = new();

        sb.AppendLine("CCVTAC SOURCE DATA:");
        sb.AppendLine($"■ Downloaded: {DateTime.Now}");
        // sb.AppendLine($"■ Service: {videoData.ExtractorKey}"); // "Youtube"
        sb.AppendLine($"■ URL: {videoData.WebpageUrl}");
        sb.AppendLine($"■ Title: {videoData.Fulltitle}");
        sb.AppendLine($"■ Uploader: {videoData.UploaderSummary()}");
        if (videoData.Creator != videoData.Uploader && !string.IsNullOrWhiteSpace(videoData.Creator))
        {
            sb.AppendLine($"■ Creator: {videoData.Creator}");
        }
        sb.AppendLine($"■ Uploaded: {videoData.FormattedUploadDate()}");
        var description = string.IsNullOrWhiteSpace(videoData.Description) ? "None." : videoData.Description;
        sb.AppendLine($"■ Video description: {description}");

        if (maybeCollectionData is CollectionMetadata collectionData)
        {
            sb.AppendLine();
            sb.AppendLine($"■ Playlist name: {collectionData.Title}");
            sb.AppendLine($"■ Playlist URL: {collectionData.WebpageUrl}");
            sb.AppendLine($"■ Playlist index: {videoData.PlaylistIndex}");
            if (!string.IsNullOrWhiteSpace(collectionData.Description))
            {
                sb.AppendLine($"■ Playlist description: {collectionData.Description}");
            }
        }

        return sb.ToString();
    }
}
