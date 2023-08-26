using System.Text.RegularExpressions;

namespace CCVTAC.Console.Downloading.DownloadEntities;

public static class DownloadEntityFactory
{
    private static readonly Dictionary<string, Regex> Patterns = new()
    {
        { nameof(Video), new Regex(@"^[\w-]{11}$|(?<=v=|v\\=)[\w-]{11}|(?<=youtu\.be/).{11}") },
        { nameof(Playlist), new Regex(@"(?<=list=)[\w\-]+") },
        { nameof(Channel), new Regex(@"(?:www\.)?youtube\.com/(?:c/|channel/|@|user/)[\w\-]+") },
    };

    public static Result<IDownloadEntity> Create(string url)
    {
        string type = string.Empty;
        string resourceId = string.Empty;

        foreach (var pair in Patterns)
        {
            var match = pair.Value.Matches(url);
            if (match is null || match.Count == 0)
                continue;
            type = pair.Key;
            resourceId = match[0].Value;
            break;
        };

        return type switch
        {
            nameof(Video) => Result.Ok((IDownloadEntity) new Video(resourceId)),
            nameof(Playlist) => Result.Ok((IDownloadEntity) new Playlist(resourceId)),
            nameof(Channel) => Result.Ok((IDownloadEntity) new Channel(resourceId)),
            _ => Result.Fail("Unsupported or invalid URL.")
        };
    }
}
