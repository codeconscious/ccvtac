using CCVTAC.Console.Downloading.Entities;

namespace CCVTAC.Tests;

public sealed class DownloadEntityFactoryTests
{
    [Fact]
    public void Video()
    {
        string url = "https://www.youtube.com/watch?v=5B1rB894B1U";
        var creationResult = DownloadEntityFactory.Create(url);
        MediaDownloadType expected = new Video("5B1rB894B1U").VideoDownloadType;
        Assert.Equal(expected, creationResult.Value.VideoDownloadType);
    }

    [Fact]
    public void VideoOnPlaylist()
    {
        string url = "https://www.youtube.com/watch?v=3pG1MvwcRUh&list=LAK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg&index=1";
        var creationResult = DownloadEntityFactory.Create(url);
        MediaDownloadType expected = new VideoOnPlaylist("3pG1MvwcRUh", "LAK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg").VideoDownloadType;
        Assert.Equal(expected, creationResult.Value.VideoDownloadType);
    }

    [Fact]
    public void Playlist()
    {
        string url = "https://www.youtube.com/playlist?list=PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z";
        var creationResult = DownloadEntityFactory.Create(url);
        MediaDownloadType expected = new Playlist("PLbGKwbAYGKxKMHoGVLhzoyPnH9rTXEH6z").VideoDownloadType;
        Assert.Equal(expected, creationResult.Value.VideoDownloadType);
    }

    [Fact]
    public void Channel()
    {
        string channelUrl = "https://www.youtube.com/channel/UCqLwLsPsfuy_vSNK09sLomw";
        var creationResult = DownloadEntityFactory.Create(channelUrl);
        MediaDownloadType expected = new Channel("UCqLwLsPsfuy_vSNK09sLomw").VideoDownloadType;
        Assert.Equal(expected, creationResult.Value.VideoDownloadType);
    }
}
