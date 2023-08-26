using System.Text.RegularExpressions;

namespace CCVTAC.Console.DownloadEntities;

public sealed class Channel : IDownloadEntity
{
    public static string Name => nameof(Channel);
    public static Regex Regex => new(@"(?:www\.)?youtube\.com/(?:c/|channel/|@|user/)[\w\-]+");
    public static string UrlBase => "https://"; // Differs because this regex will match entire URLs.

    public string ResourceId { get; init; }
    public string FullResourceId => UrlBase + ResourceId;

    public Channel(string resourceId)
    {
        ResourceId = resourceId.Trim();
    }
}
