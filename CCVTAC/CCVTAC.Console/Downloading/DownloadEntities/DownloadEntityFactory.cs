using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public static class DownloadEntityFactory
{
    private static readonly string _unsupportedUrlText = "Unsupported or invalid URL.";

    private static readonly Dictionary<DownloadType, Regex> Patterns = new()
    {
        { DownloadType.Video, new Regex(@"^[\w-]{11}$|(?<=v=|v\\=)[\w-]{11}|(?<=youtu\.be/).{11}") },
        { DownloadType.Playlist, new Regex(@"(?<=list=)[\w\-]+") },
        { DownloadType.Channel, new Regex(@"(?:www\.)?youtube\.com/(?:c/|channel/|@|user/)[\w\-]+") },
    };

    public static Result<IDownloadEntity> Create(string url)
    {
        var a = Patterns
            .Select(pattern => (pattern.Key, pattern.Value.Match(url)))
            .Where(typeAndMatch => typeAndMatch.Item2.Success)
            .Select(typeAndMatch => (typeAndMatch.Item1, typeAndMatch.Item2.Value));

        if (!a.Any())
            return Result.Fail(_unsupportedUrlText);

        var (type, resourceId) = a.First();

        return type switch
        {
            DownloadType.Video =>    Result.Ok((IDownloadEntity) new Video(resourceId)),
            DownloadType.Playlist => Result.Ok((IDownloadEntity) new Playlist(resourceId)),
            DownloadType.Channel=>   Result.Ok((IDownloadEntity) new Channel(resourceId)),
            _ => Result.Fail(_unsupportedUrlText)
        };
    }
}
