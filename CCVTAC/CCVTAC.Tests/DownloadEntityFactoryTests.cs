using CCVTAC.Console.Downloading.DownloadEntities;

namespace CCVTAC.Tests;

public sealed class DownloadEntityFactory
{
    [Fact]
    public void Video()
    {
        var videoUrl = "https://www.youtube.com/watch?v=5B1rB894B1U";
        var result = Console.Downloading.DownloadEntities.DownloadEntityFactory.Create(videoUrl);
        var expectedType = new Video("5B1rB894B1U").Type;
        Assert.Equal(expectedType, result.Value.Type);
    }

    [Fact]
    public void Playlist()
    {
        var playlistUrl = "https://www.youtube.com/playlist?list=PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z";
        var result = Console.Downloading.DownloadEntities.DownloadEntityFactory.Create(playlistUrl);
        var expectedType = new Playlist("PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z").Type;
        Assert.Equal(expectedType, result.Value.Type);
    }

    [Fact]
    public void Channel()
    {
        var channelUrl = "https://www.youtube.com/channel/UCqLwLsPsfuy_vSNK09sLomw";
        var result = Console.Downloading.DownloadEntities.DownloadEntityFactory.Create(channelUrl);
        var expectedType = new Channel("UCqLwLsPsfuy_vSNK09sLomw").Type;
        Assert.Equal(expectedType, result.Value.Type);
    }
}
