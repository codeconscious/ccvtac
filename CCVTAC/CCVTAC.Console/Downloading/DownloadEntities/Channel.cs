using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public sealed class Channel(string resourceId) : IDownloadEntity
{
    public static Regex Regex => new(@"(?:www\.)?youtube\.com/(?:c/|channel/|@|user/)[\w\-]+");
    public static string UrlBase => "https://"; // Differs because this regex will match entire URLs.

    public DownloadType Type => DownloadType.Channel;
    public string ResourceId { get; init; } = resourceId.Trim();
    public string FullResourceUrl => UrlBase + ResourceId;
}
