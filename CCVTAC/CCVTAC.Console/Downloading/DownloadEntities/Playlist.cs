using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public sealed class Playlist : IDownloadEntity
{
    public static IEnumerable<Regex> Regexes => new List<Regex>
    {
        new(@"(?<=list=)([\w\-]+)")
    };

    public DownloadType Type => DownloadType.Playlist;

    public static string UrlBase => "https://www.youtube.com/playlist?list=";
    // public string ResourceId { get; init; }
    // public string? SecondaryResourceId { get; }
    // public string FullResourceUrl => UrlBase + ResourceId;

    public ResourceUrlSet PrimaryResource { get; init; }
    public ResourceUrlSet? SupplementaryResource { get; init; }

    public Playlist(string resourceId)
    {
        PrimaryResource = new ResourceUrlSet(UrlBase, resourceId.Trim());
    }
}
