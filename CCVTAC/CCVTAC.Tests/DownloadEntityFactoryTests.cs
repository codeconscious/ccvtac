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
        Assert.Equal(expectedType, result.GetType());
    }

    [Fact]
    public void Playlist()
    {
        var playlistUrl = "https://www.youtube.com/playlist?list=PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z";
        var result = CCVTAC.Console.DownloadEntities.DownloadEntityFactory.Create(playlistUrl);
        var expectedType = new Playlist("PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z").GetType();
        Assert.Equal(expectedType, result.GetType());
    }

    [Fact]
    public void Channel()
    {
        var channelUrl = "https://www.youtube.com/channel/UCqLwLsPsfuy_vSNK09sLomw";
        var result = CCVTAC.Console.DownloadEntities.DownloadEntityFactory.Create(channelUrl);
        var expectedType = new Channel("UCqLwLsPsfuy_vSNK09sLomw").GetType();
        Assert.Equal(expectedType, result.GetType());
    }

    //[Fact]
    //public void VideoTest()
    //{
    //    var url = "https://www.youtube.com/watch?v=5B1rB894B1U";
    //    var videoDownload = new Video("5B1rB894B1U");
    //    var result = videoDownload.DoesMatch(url);
    //    Assert.True(result);
    //}
    //[Fact]
    //public void VideoTest_Invalid()
    //{
    //    var url = "https://www.youtube.com/watch?v=5B1rB894B1";
    //    var videoDownload = new Video("5B1rB894B1");
    //    var result = videoDownload.DoesMatch(url);
    //    Assert.False(result);
    //}

    //[Fact]
    //public void PlaylistTest()
    //{
    //    var url = "https://www.youtube.com/playlist?list=PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z";
    //    var playlistDownload = new Playlist("PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z");
    //    var result = playlistDownload.DoesMatch(url);
    //    Assert.True(result);
    //}

    //[Fact]
    //public void ChannelTest()
    //{
    //    var url = "https://www.youtube.com/channel/UCqLwLsPsfuy_vSNK09sLomw";
    //    var channelDownload = new Channel("UCqLwLsPsfuy_vSNK09sLomw");
    //    var result = channelDownload.DoesMatch(url);
    //    Assert.True(result);
    //}
}
