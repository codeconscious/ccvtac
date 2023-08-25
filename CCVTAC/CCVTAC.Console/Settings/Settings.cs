using System.Text.Json.Serialization;

namespace CCVTAC.Console.Settings;

public sealed class Settings
{
    [JsonPropertyName("workingDirectory")]
    [JsonRequired]
    public string? WorkingDirectory { get; init; }

    [JsonPropertyName("moveToDirectory")]
    [JsonRequired]
    public string? MoveToDirectory { get; init; }

    [JsonPropertyName("splitChapters")]
    public bool SplitChapters { get; init; } = true;

    /// <summary>
    /// The audio file format that should be downloaded. It must be
    /// a format supported by both yt-dlp and TagLib#.
    /// </summary>
    [JsonPropertyName("audioFormat")]
    public string? AudioFormat { get; init; } = "m4a";
}
