namespace CCVTAC.Console.PostProcessing.Tagging;

internal class TagDetectionSchemes
{
    public string? DetectTitle(YouTubeVideoJson.Root data, string? defaultName = null)
    {
        return Detectors.DetectSingle<string>(data, DetectionSchemeBank.Title, null) ?? defaultName;
    }

    public string? DetectArtist(YouTubeVideoJson.Root data, string? defaultArtist = null)
    {
        return Detectors.DetectSingle<string>(data, DetectionSchemeBank.Artist, null) ?? defaultArtist;
    }

    public string? DetectAlbum(YouTubeVideoJson.Root data, string? defaultAlbum = null)
    {
        return Detectors.DetectSingle<string>(data, DetectionSchemeBank.Album, null) ?? defaultAlbum;
    }

    public ushort? DetectReleaseYear(YouTubeVideoJson.Root data, ushort? defaultYear = null)
    {
        ushort output = Detectors.DetectSingle<ushort>(data, DetectionSchemeBank.Year, default);
        return output is default(ushort) ? defaultYear : output;
    }

    public string? DetectComposers(YouTubeVideoJson.Root data)
    {
        return Detectors.DetectMultiple<string>(data, DetectionSchemeBank.Composers, null, "; ");
    }
}
