namespace CCVTAC.Console.Downloading.DownloadEntities;

public enum DownloadType
{
    /// <summary>
    /// A single video.
    /// </summary>
    Video,

    /// <summary>
    /// A video whose ID is also provided with a playlist on which it resides.
    /// </summary>
    VideoOnPlaylist,

    /// <summary>
    /// A playlist containing 1 or more videos.
    /// </summary>
    Playlist,

    /// <summary>
    /// A user's channel, which contains videos and maybe playlists.
    /// </summary>
    Channel
}
