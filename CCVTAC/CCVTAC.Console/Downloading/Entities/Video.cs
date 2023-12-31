using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.Entities;

/// <summary>
/// A single video.
/// </summary>
public sealed class Video : IDownloadEntity
{
    public static IEnumerable<Regex> Regexes => new List<Regex>
    {
        new("""^([\w-]{11})$"""),
        new("""(?<=v=|v\\=)([\w-]{11})"""),
        new("""(?<=youtu\.be/)(.{11})""")
    };

    public static string UrlBase => "https://www.youtube.com/watch?v=";

    public DownloadType DownloadType => DownloadType.Media;
    public MediaDownloadType VideoDownloadType => MediaDownloadType.Video;

    public ResourceUrlSet PrimaryResource { get; init; }
    public ResourceUrlSet? SupplementaryResource { get; init; }

    public Video(string resourceId)
    {
        PrimaryResource = new ResourceUrlSet(UrlBase, resourceId.Trim());
    }
}
