using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public interface IDownloadEntity
{
    public DownloadType Type { get; }
    public static IEnumerable<Regex> Regexes { get; } = Enumerable.Empty<Regex>();

    public ResourceUrlSet PrimaryResource { get; init; }
    public ResourceUrlSet? SupplementaryResource { get; init; }

    public static bool IsMatch(string input) =>
        input is not null &&
        Regexes.Any(r => r.IsMatch(input));
}
