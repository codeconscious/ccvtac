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
    /// The file to which history should be logged.
    /// </summary>
    [JsonPropertyName("historyFilePath")]
    [JsonRequired]
    public string HistoryFilePath { get; init; } = string.Empty;

    /// <summary>
    /// How many history entries to display.
    /// </summary>
    [JsonPropertyName("historyDisplayCount")]
    public byte HistoryDisplayCount { get; init; } = 30;

    /// <summary>
    /// Specifies whether video chapters should be split into a separate
    /// audio files (true) or instead be ignored such that there is one combined
    /// audio file for the entire video (false). Defaults to true.
    /// </summary>
    [JsonPropertyName("splitChapters")]
    public bool SplitChapters { get; init; } = true;

    /// <summary>
    /// The audio file format that should be downloaded. It must be a format supported
    /// by both yt-dlp and TagLib# (the tag-editing library used by this program).
    /// </summary>
    /// <remarks>I'm only supporting M4A, which requires no conversion, for now. (See PR #21.)</remarks>
    [JsonPropertyName("audioFormat")]
    public string AudioFormat = "m4a";

    /// <summary>
    /// How many seconds to sleep between multiple downloads. This value is
    /// passed to the download tool.
    /// </summary>
    [JsonPropertyName("sleepBetweenDownloadsSeconds")]
    public ushort SleepSecondsBetweenDownloads { get; init; } = 3;

    /// <summary>
    /// How many seconds the program should sleep between multiple batches.
    /// A batch is defined as one input entry by the user.
    /// </summary>
    [JsonPropertyName("sleepBetweenBatchesSeconds")]
    public ushort SleepSecondsBetweenBatches { get; init; } = 15;

    /// <summary>
    /// A list of uploader names for whom the video upload dates' year value
    /// should not be used at the video's release year. It should contain channel names.
    /// </summary>
    [JsonPropertyName("ignoreUploadYearUploaders")]
    public string[]? IgnoreUploadYearUploaders { get; init; }

    /// <summary>
    /// Shows verbose output if true; otherwise, shows normal output.
    /// </summary>
    [JsonPropertyName("verboseOutput")]
    public bool VerboseOutput { get; init; } = false;

    /// <summary>
    /// If the supplied video uploader is specified in the settings, returns the video's upload year.
    /// Otherwise, returns null.
    /// </summary>
    public ushort? GetAppropriateReleaseDateIfAny(VideoMetadata videoData)
    {
        if (this.IgnoreUploadYearUploaders?.Contains(videoData.Uploader,
                                                     StringComparer.OrdinalIgnoreCase) == true)
        {
            return null;
        }

        return ushort.TryParse(videoData.UploadDate[0..4], out ushort parsedYear)
            ? parsedYear
            : null;
    }
}
