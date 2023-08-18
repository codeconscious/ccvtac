using System.Text.RegularExpressions;

namespace CCVTAC.Console.DownloadEntities;

public class Playlist : IDownloadEntity
{
    public static string Name => nameof(Playlist);
    public static Regex Regex => new(@"(?<=list=)[\w\-]+");
    public static string UrlBase => "https://www.youtube.com/playlist?list=";

    public string ResourceId { get; init; }
    public string FullResourceId => UrlBase + ResourceId;

    public Playlist(string resourceId)
    {
        ResourceId = resourceId.Trim();
    }
}
