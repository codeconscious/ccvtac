namespace CCVTAC.Console.Downloading.Entities;

/// <summary>
/// Types of media that can be downloaded.
/// </summary>
public enum MediaDownloadType
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
    /// A user's channel, containing 1 or more videos.
    /// </summary>
    Channel
}
