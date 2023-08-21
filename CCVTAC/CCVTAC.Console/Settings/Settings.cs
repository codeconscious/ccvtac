using System.Text.Json.Serialization;

namespace CCVTAC.Console.Settings;

public sealed record Settings
{
    [JsonPropertyName("workingDirectory")]
    [JsonRequired]
    public string? WorkingDirectory { get; set; }

    [JsonPropertyName("moveToDirectory")]
    [JsonRequired]
    public string? MoveToDirectory { get; set; }
}
