namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Provides methods to search for specific tag field data (artist, album, etc.) within video metadata.
/// </summary>
internal class TagDetector
{
    internal string? DetectTitle(YouTubeVideoJson.Root data, string? defaultName = null)
    {
        return Detectors.DetectSingle<string>(data, DetectionSchemeBank.Title, null) ?? defaultName;
    }

    internal string? DetectArtist(YouTubeVideoJson.Root data, string? defaultArtist = null)
    {
        return Detectors.DetectSingle<string>(data, DetectionSchemeBank.Artist, null) ?? defaultArtist;
    }

    internal string? DetectAlbum(YouTubeVideoJson.Root data, string? defaultAlbum = null)
    {
        return Detectors.DetectSingle<string>(data, DetectionSchemeBank.Album, null) ?? defaultAlbum;
    }

    internal string? DetectComposers(YouTubeVideoJson.Root data)
    {
        return Detectors.DetectMultiple<string>(data, DetectionSchemeBank.Composers, null, "; ");
    }

    internal ushort? DetectReleaseYear(YouTubeVideoJson.Root data, ushort? defaultYear = null)
    {
        ushort output = Detectors.DetectSingle<ushort>(data, DetectionSchemeBank.Year, default);

        return output is default(ushort) // The default ushort value indicates no match was found.
            ? defaultYear
            : output;
    }
}
