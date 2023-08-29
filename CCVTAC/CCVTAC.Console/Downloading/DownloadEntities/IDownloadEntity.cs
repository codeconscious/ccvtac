using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public interface IDownloadEntity
{
    public DownloadType Type { get; }
    public static Regex Regex { get; } = new("");
    public static string UrlBase { get; } = string.Empty;
    public string ResourceId { get; }

    public string FullResourceUrl => UrlBase + ResourceId;

    public static bool IsMatch(string input) =>
        input is not null && Regex.IsMatch(input);
}

public enum DownloadType
{
    Video,
    Playlist,
    Channel
}
