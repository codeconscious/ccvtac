using System.Text.Json.Serialization;
using CCVTAC.Console.PostProcessing;

namespace CCVTAC.Console.Settings;

/// <summary>
/// Represents the settings saved in the settings file. These customizations
/// change the behavior of the application.
/// </summary>
public sealed class UserSettings
{
    /// <summary>
    /// The directory where working files are temporarily stored during
    /// processing, before they are moved to the move-to directory.
    /// Must be a fully qualified path.
    /// </summary>
    [JsonPropertyName("workingDirectory")]
    [JsonRequired]
    public string WorkingDirectory { get; init; } = string.Empty;

    /// <summary>
    /// The directory to which the final audio files should be moved.
    /// Must be a fully qualified path.
    /// </summary>
    [JsonPropertyName("moveToDirectory")]
    [JsonRequired]
    public string MoveToDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Specifies whether video chapters should be split into a separate
    /// audio files (true) or instead be ignored such that there is one combined
    /// audio file for the entire video (false). Defaults to true.
    /// </summary>
    [JsonPropertyName("splitChapters")]
    public bool SplitChapters { get; init; } = true;

    /// <summary>
    /// The audio file format that should be downloaded. It must be
    /// a format supported by both yt-dlp and TagLib#.
    /// </summary>
    [JsonPropertyName("audioFormat")]
    public string? AudioFormat { get; init; } = "m4a";

    /// <summary>
    /// How many seconds the program should sleep between multiple downloads.
    /// </summary>
    [JsonPropertyName("sleepBetweenDownloadsSeconds")]
    public ushort SleepBetweenDownloadsSeconds { get; init; } = 3;

    /// <summary>
    /// A list of uploader names for whom the video upload dates' year value
    /// can be used at the video's release year. It should contain channel names.
    /// Case can be ignored.
    /// </summary>
    [JsonPropertyName("useUploadYearUploaders")]
    public string[]? UseUploadYearUploaders { get; init; }

    /// <summary>
    /// If the supplied video uploader is specified in the settings, returns the video's upload year.
    /// Otherwise, returns null.
    /// </summary>
    public ushort? GetVideoUploadDateIfRegisteredUploader(YouTubeJson.Root videoData)
    {
        return
            this.UseUploadYearUploaders?.Contains(videoData.uploader, StringComparer.OrdinalIgnoreCase) == true &&
            ushort.TryParse(videoData.upload_date[0..4], out var parsedYear)
                ? parsedYear
                : null;
    }
}
