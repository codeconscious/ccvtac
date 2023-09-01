using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public sealed class Playlist : IDownloadEntity
{
    public static Regex Regex => new(@"(?<=list=)[\w\-]+");
    public static string UrlBase => "https://www.youtube.com/playlist?list=";

    public DownloadType Type => DownloadType.Playlist;
    public string ResourceId { get; init; }
    public string FullResourceUrl => UrlBase + ResourceId;

    public Playlist(string resourceId)
    {
        ResourceId = resourceId.Trim();
    }
}
