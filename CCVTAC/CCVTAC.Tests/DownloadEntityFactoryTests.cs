using CCVTAC.Console.Downloading.DownloadEntities;

namespace CCVTAC.Tests;

public sealed class DownloadEntityFactoryTests
{
    [Fact]
    public void Video()
    {
        string videoUrl = "https://www.youtube.com/watch?v=5B1rB894B1U";
        var result = DownloadEntityFactory.Create(videoUrl);
        DownloadType expectedType = new Video("5B1rB894B1U").Type;
        Assert.Equal(expectedType, result.Value.Type);
    }

    [Fact]
    public void Playlist()
    {
        string playlistUrl = "https://www.youtube.com/playlist?list=PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z";
        var result = DownloadEntityFactory.Create(playlistUrl);
        DownloadType expectedType = new Playlist("PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z").Type;
        Assert.Equal(expectedType, result.Value.Type);
    }

    [Fact]
    public void Channel()
    {
        string channelUrl = "https://www.youtube.com/channel/UCqLwLsPsfuy_vSNK09sLomw";
        var result = DownloadEntityFactory.Create(channelUrl);
        DownloadType expectedType = new Channel("UCqLwLsPsfuy_vSNK09sLomw").Type;
        Assert.Equal(expectedType, result.Value.Type);
    }
}
