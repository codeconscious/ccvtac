namespace CCVTAC.Console.PostProcessing;

public static class YouTubeJsonExtensionMethods
{
    /// <summary>
    /// Returns a string summarizing video uploader information.
    /// </summary>
    public static string UploaderSummary(this YouTubeJson.Root data) =>
        $"{data.uploader} ({(!string.IsNullOrWhiteSpace(data.uploader_url) ? data.uploader_url : data.uploader_id)})";

    /// <summary>
    /// Returns a formatted version of the upload date (e.g., "08/27/2023") from the
    /// plain version (e.g., "20230827") within the parsed JSON file data.
    /// </summary>
    public static string FormattedUploadDate(this YouTubeJson.Root data) =>
        $"{data.upload_date[4..6]}/{data.upload_date[6..8]}/{data.upload_date[0..4]}";

    /// <summary>
    /// Returns a formatted comment using data parsed from the JSON file.
    /// </summary>
    public static string GenerateComment(this YouTubeJson.Root data)
    {
        System.Text.StringBuilder sb = new();
        sb.AppendLine("SOURCE DATA:");
        sb.AppendLine($"• Downloaded: {DateTime.Now} using CCVTAC");
        sb.AppendLine($"• Service: {data.extractor_key}");
        sb.AppendLine($"• URL: {data.webpage_url}");
        sb.AppendLine($"• Title: {data.fulltitle}");
        sb.AppendLine($"• Uploader: {data.UploaderSummary()}");
        sb.AppendLine($"• Uploaded: {data.FormattedUploadDate()}");
        sb.AppendLine($"• Description: {data.description})");
        return sb.ToString();
    }
}
