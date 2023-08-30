namespace CCVTAC.Console.PostProcessing;

/// <summary>
/// Contains all of the data necessary for tagging files.
/// </summary>
public sealed class TaggingSet
{
    public string ResourceId { get; init; }
    public List<string> AudioFilePaths { get; init; }
    public string JsonFilePath { get; init; }

    public TaggingSet(string resourceId, IEnumerable<string> audioFilePaths, string jsonFilePath)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            throw new ArgumentException("The resource ID must be provided");
        if (string.IsNullOrWhiteSpace(jsonFilePath))
            throw new ArgumentException("The JSON file path must be provided");

        ResourceId = resourceId.Trim();
        AudioFilePaths = audioFilePaths.ToList();
        JsonFilePath = jsonFilePath.Trim();
    }
}
