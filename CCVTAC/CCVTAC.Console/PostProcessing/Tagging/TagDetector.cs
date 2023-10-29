namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Provides methods to search for specific tag field data (artist, album, etc.) within video metadata.
/// </summary>
internal class TagDetector
{
    internal string? DetectTitle(VideoMetadata videoData, string? defaultName = null)
    {
        return Detectors.DetectSingle<string>(videoData, DetectionSchemeBank.Title, null)
               ?? defaultName;
    }

    internal string? DetectArtist(VideoMetadata videoData, string? defaultArtist = null)
    {
        return Detectors.DetectSingle<string>(videoData, DetectionSchemeBank.Artist, null)
               ?? defaultArtist;
    }

    internal string? DetectAlbum(VideoMetadata videoData, string? defaultAlbum = null)
    {
        return Detectors.DetectSingle<string>(videoData, DetectionSchemeBank.Album, null)
               ?? defaultAlbum;
    }

    internal string? DetectComposers(VideoMetadata videoData)
    {
        return Detectors.DetectMultiple<string>(videoData, DetectionSchemeBank.Composers, null, "; ");
    }

    internal ushort? DetectReleaseYear(VideoMetadata videoData, ushort? defaultYear)
    {
        ushort output = Detectors.DetectSingle<ushort>(videoData, DetectionSchemeBank.Year, default);

        return output is default(ushort) // The default ushort value indicates no match was found.
            ? defaultYear
            : output;
    }
}
