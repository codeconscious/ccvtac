namespace CCVTAC.Console.Downloading.DownloadEntities;

/// <summary>
/// The set of data necessary to create URLs for resources.
/// </summary>
public sealed record ResourceUrlSet
{
    /// <summary>
    /// The base part of the URL, which precedes the resource ID in the full URL.
    /// </summary>
    public string UrlBase { get; init; }

    /// <summary>
    /// An ID to a specific resource (e.g., video, playlist, or channel).
    /// </summary>
    public string ResourceId { get; init; }

    /// <summary>
    /// A complete URL to a resource, suitable to passing to other programs.
    /// </summary>
    public string FullResourceUrl => UrlBase + ResourceId;

    public ResourceUrlSet(string urlBase, string resourceId)
    {
        UrlBase = urlBase;
        ResourceId = resourceId;
    }
}
