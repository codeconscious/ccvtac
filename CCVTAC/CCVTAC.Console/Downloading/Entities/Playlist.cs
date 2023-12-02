using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.Entities;

/// <summary>
/// A single playlist which contains at least one video.
/// </summary>
public sealed class Playlist : IDownloadEntity
{
    public static IEnumerable<Regex> Regexes => new List<Regex>
    {
        new(@"(?<=list=)([\w\-]+)")
    };

    public DownloadType DownloadType => DownloadType.Media;
    public MediaDownloadType VideoDownloadType => MediaDownloadType.Playlist;

    public static string UrlBase => "https://www.youtube.com/playlist?list=";

    public ResourceUrlSet PrimaryResource { get; init; }
    public ResourceUrlSet? SupplementaryResource { get; init; }

    public Playlist(string resourceId)
    {
        PrimaryResource = new ResourceUrlSet(UrlBase, resourceId.Trim());
    }
}
