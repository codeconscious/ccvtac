using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public class VideoOnPlaylist : IDownloadEntity
{
    public static IEnumerable<Regex> Regexes => new List<Regex>
    {
        new("""(?<=v=|v\=)([\w-]{11})(?:&list=([\w_-]+))""")
    };

    public DownloadType Type => DownloadType.VideoOnPlaylist;

    public static string UrlBase => "https://www.youtube.com/watch?v=";

    public ResourceUrlSet PrimaryResource { get; init; }
    public ResourceUrlSet? SupplementaryResource { get; init; }

    public VideoOnPlaylist(string resourceId, string secondaryResourceId)
    {
        PrimaryResource = new ResourceUrlSet(UrlBase, resourceId.Trim());
        SupplementaryResource = new ResourceUrlSet("https://www.youtube.com/playlist?list=", secondaryResourceId.Trim());
    }
}
