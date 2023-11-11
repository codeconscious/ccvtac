namespace CCVTAC.Console.Downloading.DownloadEntities;

public class ResourceUrlSet
{
    public string UrlBase { get; init; }
    public string ResourceId { get; init; }
    public string FullResourceUrl => UrlBase + ResourceId;

    public ResourceUrlSet(string urlBase, string resourceId)
    {
        UrlBase = urlBase;
        ResourceId = resourceId;
    }
}
