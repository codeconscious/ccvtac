using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

/// <summary>
/// An interface representing a media resource that can be downloaded.
/// </summary>
public interface IDownloadEntity
{
    public DownloadType DownloadType { get; }
    public MediaDownloadType VideoDownloadType { get; }
    public static IEnumerable<Regex> Regexes { get; } = Enumerable.Empty<Regex>();

    public ResourceUrlSet PrimaryResource { get; init; }
    public ResourceUrlSet? SupplementaryResource { get; init; }

    public static bool IsMatch(string input) =>
        input is not null &&
        Regexes.Any(r => r.IsMatch(input));
}
