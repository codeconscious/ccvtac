using CCVTAC.Console.DownloadEntities;

namespace CCVTAC.Tests;

public class DownloadEntityFactory
{
    [Fact]
    public void Video()
    {
        var videoUrl = "https://www.youtube.com/watch?v=5B1rB894B1U";
        var result = CCVTAC.Console.DownloadEntities.DownloadEntityFactory.Create(videoUrl);
        var expectedType = new Video("5B1rB894B1U").GetType();
        Assert.Equal(expectedType, result.Value.GetType());
    }

    [Fact]
    public void Playlist()
    {
        var playlistUrl = "https://www.youtube.com/playlist?list=PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z";
        var result = CCVTAC.Console.DownloadEntities.DownloadEntityFactory.Create(playlistUrl);
        var expectedType = new Playlist("PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z").GetType();
        Assert.Equal(expectedType, result.Value.GetType());
    }

    [Fact]
    public void Channel()
    {
        var channelUrl = "https://www.youtube.com/channel/UCqLwLsPsfuy_vSNK09sLomw";
        var result = CCVTAC.Console.DownloadEntities.DownloadEntityFactory.Create(channelUrl);
        var expectedType = new Channel("UCqLwLsPsfuy_vSNK09sLomw").GetType();
        Assert.Equal(expectedType, result.Value.GetType());
    }
}
