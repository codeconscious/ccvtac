using System.Text.RegularExpressions;

namespace CCVTAC.Console.DownloadEntities;

public class Video : IDownloadEntity
{
    public static string Name => nameof(Video);
    public static Regex Regex => new(@"^[\w-]{11}$|(?<=v=|v\\=)[\w-]{11}|(?<=youtu\.be/).{11}");
    public static string UrlBase => "https://www.youtube.com/watch?v=";

    public string ResourceId { get; init; }
    public string FullResourceId => UrlBase + ResourceId;

    public Video(string resourceId)
    {
        ResourceId = resourceId.Trim();
    }
}
