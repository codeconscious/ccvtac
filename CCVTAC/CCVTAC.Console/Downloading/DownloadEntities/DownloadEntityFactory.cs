using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

/// <summary>
/// Determines and creates the appropriate download type for a given URl.
/// </summary>
public static class DownloadEntityFactory
{
    private static readonly Dictionary<DownloadType, IEnumerable<Regex>> Patterns = new()
    {
        { DownloadType.VideoOnPlaylist, VideoOnPlaylist.Regexes },
        { DownloadType.Video,           Video.Regexes },
        { DownloadType.Playlist,        Playlist.Regexes },
        { DownloadType.Channel,         Channel.Regexes }
    };

    /// <summary>
    /// Creates an IDownloadEntity, which is used for download operations.
    /// </summary>
    /// <param name="url"></param>
    public static Result<IDownloadEntity> Create(string url)
    {
        List<(DownloadType type, string resourceId, string? supplementaryResourceId)> typesWithResourceIds =
            Patterns.SelectMany(pattern =>  pattern.Value.Select(regex => (pattern.Key, regex.Match(url.Trim())))
                    .Where(typeAndMatch =>  typeAndMatch.Item2.Success)
                    .Select(typeAndMatch => (
                        typeAndMatch.Key,
                        typeAndMatch.Item2.Groups[1].Value,
                        typeAndMatch.Item2.Groups[2]?.Value)))
                    .ToList();

        if (typesWithResourceIds.IsEmpty())
        {
            return Result.Fail("Unsupported or invalid URL. (No matching URL found.)");
        }

        if (typesWithResourceIds.Count > 1)
        {
            return Result.Fail("More than one matching regex pattern was unexpectedly found.");
        }

        (DownloadType type, string resourceId, string? supplementaryResourceId) = typesWithResourceIds.Single();

        return type switch
        {
            DownloadType.VideoOnPlaylist => Result.Ok((IDownloadEntity) new VideoOnPlaylist(resourceId, supplementaryResourceId!)),
            DownloadType.Video =>           Result.Ok((IDownloadEntity) new Video(resourceId)),
            DownloadType.Playlist =>        Result.Ok((IDownloadEntity) new Playlist(resourceId)),
            DownloadType.Channel=>          Result.Ok((IDownloadEntity) new Channel(resourceId)),
            _ =>                            Result.Fail("Unsupported or invalid URL. (No matching download type found.)")
        };
    }
}
