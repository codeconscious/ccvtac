using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public sealed class Video(string resourceId) : IDownloadEntity
{
    public static Regex Regex => new(@"^[\w-]{11}$|(?<=v=|v\\=)[\w-]{11}|(?<=youtu\.be/).{11}");
    public static string UrlBase => "https://www.youtube.com/watch?v=";

    public DownloadType Type => DownloadType.Video;
    public string ResourceId { get; init; } = resourceId.Trim();
    public string FullResourceUrl => UrlBase + ResourceId;
}
