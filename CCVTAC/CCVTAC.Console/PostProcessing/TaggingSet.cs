using System.Text.RegularExpressions;

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

    public static List<TaggingSet> CreateTaggingSets(IEnumerable<string> filePaths)
    {
        if (filePaths is null || !filePaths.Any())
            return new List<TaggingSet>();

        var regex = new Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)");
        const string jsonFileExtension = ".json";
        const string audioFileExtension = ".m4a";

        return filePaths
                    .Select(f => regex.Match(f))
                    .Where(m => m.Success)
                    .Select(m => m.Captures.OfType<Match>().First())
                    .GroupBy(m => m.Groups[1].Value, // Resource ID
                                m => m.Groups[0].Value) // Full filename
                    .Where(gr => gr.Count(f => f.EndsWith(jsonFileExtension, StringComparison.OrdinalIgnoreCase)) == 1)
                    .Select(gr => {
                        return new TaggingSet(
                            gr.Key,
                            gr.Where(f => f.EndsWith(audioFileExtension)), // # TODO: Support multiple formats.
                            gr.Where(f => f.EndsWith(jsonFileExtension)).First());
                    }).ToList();
    }
}
