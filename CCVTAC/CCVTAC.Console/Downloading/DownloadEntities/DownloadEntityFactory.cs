using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

/// <summary>
/// Determines and creates the appropriate download type for a given URl.
/// </summary>
public static class DownloadEntityFactory
{
    private static readonly string _unsupportedUrlMessage = "Unsupported or invalid URL.";

    private static readonly Dictionary<DownloadType, IEnumerable<Regex>> Patterns = new()
    {
        { DownloadType.VideoOnPlaylist, VideoOnPlaylist.Regexes },
        { DownloadType.Video,           Video.Regexes },
        { DownloadType.Playlist,        Playlist.Regexes },
        { DownloadType.Channel,         Channel.Regexes }
    };

    public static Result<IDownloadEntity> Create(string url)
    {
        List<(DownloadType Key, string Value, string? Supplementary)> typesWithResourceIds =
            Patterns.SelectMany(pattern =>  pattern.Value.Select(regex => (pattern.Key, regex.Match(url)))
                    .Where(typeAndMatch =>  typeAndMatch.Item2.Success)
                    .Select(typeAndMatch => (
                        typeAndMatch.Key,
                        typeAndMatch.Item2.Groups[1].Value,
                        typeAndMatch.Item2.Groups[2]?.Value)))
                    .ToList();

        if (!typesWithResourceIds.Any())
            return Result.Fail(_unsupportedUrlMessage);

        (DownloadType type, string resourceId, string? supplementaryResourceId) = typesWithResourceIds.First();

        return type switch
        {
            DownloadType.VideoOnPlaylist => Result.Ok((IDownloadEntity) new VideoOnPlaylist(resourceId, supplementaryResourceId!)),
            DownloadType.Video =>           Result.Ok((IDownloadEntity) new Video(resourceId)),
            DownloadType.Playlist =>        Result.Ok((IDownloadEntity) new Playlist(resourceId)),
            DownloadType.Channel=>          Result.Ok((IDownloadEntity) new Channel(resourceId)),
            _ =>                            Result.Fail(_unsupportedUrlMessage)
        };
    }
}
