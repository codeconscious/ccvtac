// module CCVTAC.FSharp.Tests.TaggingTests
module TagDetectionTests

open Xunit
open CCVTAC.Console.PostProcessing.Tagging
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.Settings.Settings
open System

let emptyVideoMetadata = {
    Id = String.Empty
    Title = String.Empty
    Thumbnail = String.Empty
    Description = String.Empty
    ChannelId = String.Empty
    ChannelUrl = String.Empty
    Duration = System.Nullable 0
    ViewCount = System.Nullable 0
    AgeLimit = System.Nullable 0
    WebpageUrl = String.Empty
    Categories = []
    Tags = []
    PlayableInEmbed = Nullable<bool> false
    LiveStatus = String.Empty
    ReleaseTimestamp = System.Nullable 0
    FormatSortFields = []
    Album = String.Empty
    Artist = String.Empty
    Track = String.Empty
    CommentCount = System.Nullable 0
    LikeCount = System.Nullable 0
    Channel = String.Empty
    ChannelFollowerCount = System.Nullable 0
    ChannelIsVerified = Nullable<bool> false
    Uploader = String.Empty
    UploaderId = String.Empty
    UploaderUrl = String.Empty
    UploadDate = String.Empty
    Creator = String.Empty
    AltTitle = String.Empty
    Availability = String.Empty
    WebpageUrlBasename = String.Empty
    WebpageUrlDomain = String.Empty
    Extractor = String.Empty
    ExtractorKey = String.Empty
    PlaylistCount = System.Nullable 0
    Playlist = String.Empty
    PlaylistId = String.Empty
    PlaylistTitle = String.Empty
    NEntries = System.Nullable 0
    PlaylistIndex = Nullable<uint32> 0u
    DisplayId = String.Empty
    Fulltitle = String.Empty
    DurationString = String.Empty
    ReleaseDate = String.Empty
    ReleaseYear = Nullable<uint32> 0u
    IsLive = Nullable<bool> false
    WasLive = Nullable<bool> false
    Epoch = System.Nullable 0
    Asr = System.Nullable 0
    Filesize = System.Nullable 0
    FormatId = String.Empty
    FormatNote = String.Empty
    SourcePreference = System.Nullable 0
    AudioChannels = System.Nullable 0
    Quality = Nullable<double> 0
    HasDrm = Nullable<bool> false
    Tbr = Nullable<double> 0
    Url = String.Empty
    LanguagePreference = System.Nullable 0
    Ext = String.Empty
    Vcodec = String.Empty
    Acodec = String.Empty
    Container = String.Empty
    Protocol = String.Empty
    Resolution = String.Empty
    AudioExt = String.Empty
    VideoExt = String.Empty
    Vbr = System.Nullable 0
    Abr = Nullable<double> 0
    Format = String.Empty
    Type = String.Empty
}

[<Fact>]
let ``Detects album name in video description`` () =
    let testAlbumName = "Test Album Name"
    let videoMetadata = { emptyVideoMetadata with Description = $" album: {testAlbumName}" }
    let fallback : string option = None

    let detectionPattern : TagDetectionPattern = {
        RegexPattern = "(?<=[Aa]lbum: ).+"
        MatchGroup = 0
        SearchField = "description"
        Summary = Some "Find album name in description"
    }

    let tagDetectionPatterns : TagDetectionPatterns = {
        Title = [||]
        Artist = [||]
        Album = [| detectionPattern |]
        Composer = [||]
        Year = [||]
    }

    let result = TagDetection.detectAlbum videoMetadata fallback tagDetectionPatterns

    match result with
    | Some r -> Assert.Equal(testAlbumName, r)
    | None -> Assert.Fail $"Expected album name \"{testAlbumName}\" was not found"
