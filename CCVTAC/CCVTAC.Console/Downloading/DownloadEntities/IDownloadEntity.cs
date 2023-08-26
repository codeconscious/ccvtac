using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public interface IDownloadEntity
{
    public static string Name { get; } = string.Empty;
    public static Regex Regex { get; } = new("");
    public static string UrlBase { get; } = string.Empty;
    public string ResourceId { get; }

    public string FullResourceId => UrlBase + ResourceId;

    public static bool IsMatch(string input) =>
        input is not null && Regex.IsMatch(input);
}
