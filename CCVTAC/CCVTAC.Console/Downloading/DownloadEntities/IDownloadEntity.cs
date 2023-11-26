using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

/// <summary>
/// An interface representing a media resource that can be downloaded.
/// </summary>
public interface IDownloadEntity
{
    public static IEnumerable<Regex> Regexes { get; } = Enumerable.Empty<Regex>();

    public DownloadType DownloadType { get; }
    public MediaDownloadType VideoDownloadType { get; }

    /// <summary>
    /// The ID of the primary resource to be downloaded.
    /// </summary>
    public ResourceUrlSet PrimaryResource { get; init; }

    /// <summary>
    /// Optional ID of a supplementary resource to be downloaded.
    /// (This will generally be the playlist to which a single video is attached.)
    /// </summary>
    public ResourceUrlSet? SupplementaryResource { get; init; }
}
