using CCVTAC.Console.Downloading.DownloadEntities;

namespace CCVTAC.Tests;

public sealed class DownloadEntityFactoryTests
{
    [Fact]
    public void Video()
    {
        string videoUrl = "https://www.youtube.com/watch?v=5B1rB894B1U";
        var videoResult = DownloadEntityFactory.Create(videoUrl);
        MediaDownloadType expectedType = new Video("5B1rB894B1U").VideoDownloadType;
        Assert.Equal(expectedType, videoResult.Value.VideoDownloadType);
    }

    [Fact]
    public void Playlist()
    {
        string playlistUrl = "https://www.youtube.com/playlist?list=PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z";
        var playlistResult = DownloadEntityFactory.Create(playlistUrl);
        MediaDownloadType expectedType = new Playlist("PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z").VideoDownloadType;
        Assert.Equal(expectedType, playlistResult.Value.VideoDownloadType);
    }

    [Fact]
    public void Channel()
    {
        string channelUrl = "https://www.youtube.com/channel/UCqLwLsPsfuy_vSNK09sLomw";
        var channelResult = DownloadEntityFactory.Create(channelUrl);
        MediaDownloadType expectedType = new Channel("UCqLwLsPsfuy_vSNK09sLomw").VideoDownloadType;
        Assert.Equal(expectedType, channelResult.Value.VideoDownloadType);
    }
}
