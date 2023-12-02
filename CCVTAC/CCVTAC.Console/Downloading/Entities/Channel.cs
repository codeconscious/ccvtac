using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.Entities;

/// <summary>
/// A user's channel, which contains at least one video.
/// </summary>
public sealed class Channel : IDownloadEntity
{
    public static IEnumerable<Regex> Regexes => new List<Regex>
    {
        // URL reference: https://help.unmetric.com/en/articles/1330396-getting-a-youtube-channel-url#h_19f80839bc
        new("""((?:www\.)?youtube\.com\/(?:channel\/|c\/|user\/|@)(?:[\w\-]+))""")
    };

    public DownloadType DownloadType => DownloadType.Media;
    public MediaDownloadType VideoDownloadType => MediaDownloadType.Channel;

    // This URL base differs because the channel regex matches entire URLs.
    public static string UrlBase => "https://";

    public ResourceUrlSet PrimaryResource { get; init; }
    public ResourceUrlSet? SupplementaryResource { get; init; }

    public Channel(string resourceId)
    {
        PrimaryResource = new ResourceUrlSet(UrlBase, resourceId.Trim());
    }
}
