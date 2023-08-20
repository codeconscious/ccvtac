using System.Text.Json.Serialization;

namespace CCVTAC.Console.Settings;

public sealed record Settings
{
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    [JsonPropertyName("moveToDirectory")]
    public string? MoveToDirectory { get; set; }
}
