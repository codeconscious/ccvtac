using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.Entities;

/// <summary>
/// A single release-based playlist which contains at least one video.
/// </summary>
public sealed class ReleasePlaylist : IDownloadEntity
{
    public static IEnumerable<Regex> Regexes => new List<Regex>
    {
        new(@"(?<=list=)(O[\w\-]+)")
    };

    public DownloadType DownloadType => DownloadType.Media;
    public MediaDownloadType VideoDownloadType => MediaDownloadType.ReleasePlaylist;

    public static string UrlBase => "https://www.youtube.com/playlist?list=";

    public ResourceUrlSet PrimaryResource { get; init; }
    public ResourceUrlSet? SupplementaryResource { get; init; }

    public ReleasePlaylist(string resourceId)
    {
        PrimaryResource = new ResourceUrlSet(UrlBase, resourceId.Trim());
    }
}
