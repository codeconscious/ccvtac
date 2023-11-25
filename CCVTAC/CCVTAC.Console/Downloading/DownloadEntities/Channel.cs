using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public sealed class Channel : IDownloadEntity
{
    public static IEnumerable<Regex> Regexes => new List<Regex>
    {
        new(@"(?:www\.)?youtube\.com/(?:c/|channel/|@|user/)([\w\-]+)")
    };

    public DownloadType Type => DownloadType.Channel;

    // This URL base differs because the channel regex matches entire URLs.
    public static string UrlBase => "https://";

    // public string ResourceId { get; init; }
    // public string? SecondaryResourceId { get; }
    // public string FullResourceUrl => UrlBase + ResourceId;

    public ResourceUrlSet PrimaryResource { get; init; }
    public ResourceUrlSet? SupplementaryResource { get; init; }

    public Channel(string resourceId)
    {
        PrimaryResource = new ResourceUrlSet(UrlBase, resourceId.Trim());
    }
}
