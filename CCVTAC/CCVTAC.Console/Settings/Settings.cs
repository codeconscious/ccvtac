using System.Text.Json.Serialization;

namespace CCVTAC.Console.Settings;

public sealed record Settings
{
    [JsonPropertyName("workingDirectory")]
    [JsonRequired]
    public string? WorkingDirectory { get; init; }

    [JsonPropertyName("moveToDirectory")]
    [JsonRequired]
    public string? MoveToDirectory { get; init; }

    [JsonPropertyName("splitChapters")]
    public bool SplitChapters { get; init; } = true;
}
