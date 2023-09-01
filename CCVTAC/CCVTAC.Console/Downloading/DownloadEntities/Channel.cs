using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public sealed class Channel : IDownloadEntity
{
    public static Regex Regex => new(@"(?:www\.)?youtube\.com/(?:c/|channel/|@|user/)[\w\-]+");
    public static string UrlBase => "https://"; // Differs because this regex will match entire URLs.

    public DownloadType Type => DownloadType.Channel;
    public string ResourceId { get; init; }
    public string FullResourceUrl => UrlBase + ResourceId;

    public Channel(string resourceId)
    {
        ResourceId = resourceId.Trim();
    }
}
