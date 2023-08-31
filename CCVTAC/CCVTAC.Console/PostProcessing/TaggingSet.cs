using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing;

/// <summary>
/// Contains all of the data necessary for tagging files.
/// </summary>
public readonly record struct TaggingSet
{
    /// <summary>
    /// The ID of a download resource (i.e., a video).
    /// </summary>
    public string ResourceId { get; init; }

    /// <summary>
    /// All audio files for the associated resource ID. Several indicate
    /// that the original video was split into several audio files.
    /// </summary>
    public ImmutableHashSet<string> AudioFilePaths { get; init; }

    /// <summary>
    /// The path to the JSON file containing metadata about the source video.
    /// </summary>
    public string JsonFilePath { get; init; }

    /// <summary>
    /// The path to the image file containing the thumbnail of the source video.
    /// </summary>
    public string ImageFilePath { get; init; }

    public TaggingSet(
        string resourceId,
        IEnumerable<string> audioFilePaths,
        string jsonFilePath,
        string imageFilePath)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            throw new ArgumentException("The resource ID must be provided.");
        if (string.IsNullOrWhiteSpace(jsonFilePath))
            throw new ArgumentException("The JSON file path must be provided.");
        if (string.IsNullOrWhiteSpace(imageFilePath))
            throw new ArgumentException("The image file path must be provided.");

        ResourceId = resourceId.Trim();
        AudioFilePaths = audioFilePaths.ToImmutableHashSet();
        JsonFilePath = jsonFilePath.Trim();
        ImageFilePath = imageFilePath.Trim();
    }

    /// <summary>
    /// Create a collection of TaggingSets from a collection of filePaths
    /// related to several resource (video) IDs.
    /// </summary>
    /// <param name="filePaths">
    ///     A collection of file paths. Expected to contain 1 JSON file and
    ///     1 image file for each unique resource ID.
    /// </param>
    public static ImmutableList<TaggingSet> CreateTaggingSets(IEnumerable<string> filePaths)
    {
        if (filePaths is null || !filePaths.Any())
            return Enumerable.Empty<TaggingSet>().ToImmutableList();

        var regex = new Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)");
        const string jsonFileExtension = ".json";
        const string audioFileExtension = ".m4a";  // TODO: Support multiple formats.
        const string imageFileExtension = ".jpg";

        return filePaths
                    .Select(f => regex.Match(f))
                    .Where(m => m.Success)
                    .Select(m => m.Captures.OfType<Match>().First())
                    .GroupBy(m => m.Groups[1].Value, // Resource ID
                             m => m.Groups[0].Value) // Full filename
                    .Where(gr =>
                        gr.Count(f => f.EndsWith(jsonFileExtension, StringComparison.OrdinalIgnoreCase)) == 1 &&
                        gr.Count(f => f.EndsWith(imageFileExtension, StringComparison.OrdinalIgnoreCase)) == 1)
                    .Select(gr => {
                        return new TaggingSet(
                            gr.Key,
                            gr.Where(f => f.EndsWith(audioFileExtension)),
                            gr.Where(f => f.EndsWith(jsonFileExtension)).First(),
                            gr.Where(f => f.EndsWith(imageFileExtension)).First()
                        );
                    }).ToImmutableList();
    }
}
