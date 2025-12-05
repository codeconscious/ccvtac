module TagDetectionTests

open CCVTAC.Console.PostProcessing.Tagging
open CCVTAC.Console.PostProcessing
open CCVTAC.Console.Settings.Settings
open System
open Xunit

let emptyVideoMetadata = {
    Id = String.Empty
    Title = String.Empty
    Thumbnail = String.Empty
    Description = String.Empty
    ChannelId = String.Empty
    ChannelUrl = String.Empty
    Duration = Nullable 0
    ViewCount = Nullable 0
    AgeLimit = Nullable 0
    WebpageUrl = String.Empty
    Categories = []
    Tags = []
    PlayableInEmbed = Nullable false
    LiveStatus = String.Empty
    ReleaseTimestamp = Nullable 0
    FormatSortFields = []
    Album = String.Empty
    Artist = String.Empty
    Track = String.Empty
    CommentCount = Nullable 0
    LikeCount = Nullable 0
    Channel = String.Empty
    ChannelFollowerCount = Nullable 0
    ChannelIsVerified = Nullable false
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
    PlaylistCount = Nullable 0
    Playlist = String.Empty
    PlaylistId = String.Empty
    PlaylistTitle = String.Empty
    NEntries = Nullable 0
    PlaylistIndex = Nullable 0u
    DisplayId = String.Empty
    Fulltitle = String.Empty
    DurationString = String.Empty
    ReleaseDate = String.Empty
    ReleaseYear = Nullable 0u
    IsLive = Nullable false
    WasLive = Nullable false
    Epoch = Nullable 0
    Asr = Nullable 0
    Filesize = Nullable 0
    FormatId = String.Empty
    FormatNote = String.Empty
    SourcePreference = Nullable 0
    AudioChannels = Nullable 0
    Quality = Nullable 0
    HasDrm = Nullable false
    Tbr = Nullable 0
    Url = String.Empty
    LanguagePreference = Nullable 0
    Ext = String.Empty
    Vcodec = String.Empty
    Acodec = String.Empty
    Container = String.Empty
    Protocol = String.Empty
    Resolution = String.Empty
    AudioExt = String.Empty
    VideoExt = String.Empty
    Vbr = Nullable 0
    Abr = Nullable 0
    Format = String.Empty
    Type = String.Empty
}

let newLine = Environment.NewLine

[<Fact>]
let ``Detects album name in video description`` () =
    let testArtist = "Test Artist Name (日本語入り）"
    let testAlbum = "Test Album Name (日本語入り）"
    let testTitle = "Test Title (日本語入り）"
    let testComposer = "Test Composer (日本語入り）"
    let testYear = 1945u

    let videoMetadata = {
        emptyVideoMetadata with
            Title = $"{testArtist}「{testTitle}」"
            Description = $"album: {testAlbum}{newLine}℗ %d{testYear}{newLine}Composed by: {testComposer}" }

    let artistPattern = {
        RegexPattern = "^(.+?)「(.+)」"
        MatchGroup = 1
        SearchField = "title"
        Summary = Some "Find artist in video title"
    }

    let titlePattern = {
        artistPattern with
            MatchGroup = 2
            Summary = Some "Find title in the video title"
    }

    let albumPattern = {
        RegexPattern = "(?<=[Aa]lbum: ).+"
        MatchGroup = 0
        SearchField = "description"
        Summary = Some "Find album in description"
    }

    let composerPattern = {
        RegexPattern = "(?<=[Cc]omposed by |[Cc]omposed by: |[Cc]omposer: |作曲[:：・]).+"
        MatchGroup = 0
        SearchField = "description"
        Summary = Some "Find composer in description"
    }

    let yearPattern = {
        RegexPattern = "(?<=℗ )[12]\d{3}"
        MatchGroup = 0
        SearchField = "description"
        Summary = Some "Find year in description"
    }

    let tagDetectionPatterns = {
        Title = [| titlePattern |]
        Artist = [| artistPattern  |]
        Album = [| albumPattern |]
        Composer = [| composerPattern |]
        Year = [| yearPattern |]
    }

    match TagDetection.detectArtist videoMetadata None tagDetectionPatterns with
    | Some artistResult -> Assert.Equal(testArtist, artistResult)
    | None -> Assert.Fail $"Expected artist \"{testArtist}\" was not found."

    match TagDetection.detectAlbum videoMetadata None tagDetectionPatterns with
    | Some albumResult -> Assert.Equal(testAlbum, albumResult)
    | None -> Assert.Fail $"Expected album \"{testAlbum}\" was not found."

    match TagDetection.detectTitle videoMetadata None tagDetectionPatterns with
    | Some titleResult -> Assert.Equal(testTitle, titleResult)
    | None -> Assert.Fail $"Expected title \"{testTitle}\" was not found."

    match TagDetection.detectComposers videoMetadata tagDetectionPatterns with
    | Some composerResult -> Assert.Equal(testComposer, composerResult)
    | None -> Assert.Fail $"Expected composer \"{testComposer}\" was not found."

    match TagDetection.detectReleaseYear videoMetadata None tagDetectionPatterns with
    | Some yearResult -> Assert.Equal(testYear, yearResult)
    | None -> Assert.Fail $"Expected year \"%d{testYear}\" was not found."
