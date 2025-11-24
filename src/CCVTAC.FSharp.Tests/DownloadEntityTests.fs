module DownloadEntityTests

open Xunit
open CCVTAC.Console.Downloading
open CCVTAC.Console.Downloading.Downloading

module MediaTypeWithIdsTests =
    let incorrectMediaType = "Incorrect media type"
    let unexpectedError e = $"Unexpected error: {e}"
    let unexpectedOk = "Unexpectedly parsed a MediaType"

    [<Fact>]
    let ``Detects video URL with its ID`` () =
        let url = "https://www.youtube.com/watch?v=12312312312"
        let expectedId = "12312312312"
        let result = mediaTypeWithIds url

        match result with
        | Ok mediaType -> match mediaType with
                          | Video actualId -> Assert.Equal(expectedId, actualId)
                          | _ -> failwith incorrectMediaType
        | Error e -> failwith (unexpectedError e)

    [<Fact>]
    let ``Detects playlist video URL with its ID`` () =
        let url = "https://www.youtube.com/watch?v=12312312312&list=OLZK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg&index=1"
        let expectedVideoId = "12312312312"
        let expectedPlaylistId = "OLZK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg"
        let result = mediaTypeWithIds url

        match result with
        | Ok mediaType -> match mediaType with
                          | PlaylistVideo (actualVideoId, actualPlaylistId) ->
                              Assert.Equal(expectedVideoId, actualVideoId)
                              Assert.Equal(expectedPlaylistId, actualPlaylistId)
                          | _ -> failwith incorrectMediaType
        | Error e -> failwith (unexpectedError e)

    [<Fact>]
    let ``Detects standard playlist URL with its ID`` () =
        let url = "https://www.youtube.com/playlist?list=PLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L&index=1"
        let expectedId = "PLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"
        let result = mediaTypeWithIds url

        match result with
        | Ok mediaType -> match mediaType with
                          | StandardPlaylist actualId -> Assert.Equal(expectedId, actualId)
                          | _ -> failwith incorrectMediaType
        | Error e -> failwith (unexpectedError e)

    [<Fact>]
    let ``Detects release playlist URL with its ID`` () =
        let url = "https://www.youtube.com/playlist?list=OLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L&index=1"
        let expectedId = "OLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"
        let result = mediaTypeWithIds url

        match result with
        | Ok mediaType -> match mediaType with
                          | ReleasePlaylist actualId -> Assert.Equal(expectedId, actualId)
                          | _ -> failwith incorrectMediaType
        | Error e -> failwith (unexpectedError e)

    [<Fact>]
    let ``Detects channel URL type 1 with its ID`` () =
        let url = "https://www.youtube.com/channel/UBMmt12UKW571UWtJAgWkWrg"
        let expectedId = "www.youtube.com/channel/UBMmt12UKW571UWtJAgWkWrg"
        let result = mediaTypeWithIds url

        match result with
        | Ok mediaType -> match mediaType with
                          | Channel actualId -> Assert.Equal(expectedId, actualId)
                          | _ -> failwith incorrectMediaType
        | Error e -> failwith (unexpectedError e)

    [<Fact>]
    let ``Detects channel URL type 2 with its ID`` () =
        let url = "https://www.youtube.com/@NicknameBasedYouTubeChannelName"
        let expectedId = "www.youtube.com/@NicknameBasedYouTubeChannelName"
        let result = mediaTypeWithIds url

        match result with
        | Ok mediaType -> match mediaType with
                          | Channel actualId -> Assert.Equal(expectedId, actualId)
                          | _ -> failwith incorrectMediaType
        | Error e -> failwith (unexpectedError e)

    [<Fact>]
    let ``Detects channel URL type 2 with encoded Japanese characters`` () =
        let url = "https://www.youtube.com/@%E3%81%8A%E3%81%91%E3%83%91%E3%83%A9H"
        let expectedId = "www.youtube.com/@%E3%81%8A%E3%81%91%E3%83%91%E3%83%A9H"
        let result = mediaTypeWithIds url

        match result with
        | Ok mediaType -> match mediaType with
                          | Channel actualId -> Assert.Equal(expectedId, actualId)
                          | _ -> failwith incorrectMediaType
        | Error e -> failwith (unexpectedError e)

    [<Fact>]
    let ``Detects channel videos URL with encoded Japanese characters`` () =
        let url = "https://www.youtube.com/@%E3%81%8A%91%E3%83%A9H/videos"
        let expectedId = "www.youtube.com/@%E3%81%8A%91%E3%83%A9H/videos"
        let result = mediaTypeWithIds url

        match result with
        | Ok mediaType -> match mediaType with
                          | Channel actualId -> Assert.Equal(expectedId, actualId)
                          | _ -> failwith incorrectMediaType
        | Error e -> failwith (unexpectedError e)

    [<Fact>]
    let ``Detects unsupported channel URL type 2 with unencoded Japanese characters`` () =
        let url = "https://www.youtube.com/@日本語"
        let result = mediaTypeWithIds url

        match result with
        | Error _ -> Assert.True true
        | Ok _ -> Assert.True (false, unexpectedOk)

    [<Fact>]
    let ``Detects unsupported channel videos URL with unencoded Japanese characters`` () =
        let url = "https://www.youtube.com/@日本語/videos"
        let result = mediaTypeWithIds url

        match result with
        | Error _ -> Assert.True true
        | Ok _ -> Assert.True (false, unexpectedOk)

    [<Fact>]
    let ``Error result when an invalid URL is passed`` () =
        let url = "INVALID URL"
        let result = mediaTypeWithIds url

        match result with
        | Error _ -> Assert.True true
        | Ok _ -> Assert.True (false, unexpectedOk)

module DownloadUrlsTests =
    [<Fact>]
    let ``Generates expected URL for video`` () =
        let video = Video "12312312312"
        let expectedUrl = ["https://www.youtube.com/watch?v=12312312312"]
        let result = extractDownloadUrls video
        Assert.Equal(result.Length, 1)
        Assert.Equal(expectedUrl.Length, result.Length)
        Assert.Equal(expectedUrl.Head, result.Head)

    [<Fact>]
    let ``Generates expected URL pair for playlist video`` () =
        let playlistVideo = PlaylistVideo ("12312312312", "OLZK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg")
        let videoUrl = "https://www.youtube.com/watch?v=12312312312"
        let playlistUrl = "https://www.youtube.com/playlist?list=OLZK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg"
        let expectedUrls = [videoUrl; playlistUrl]
        let result = extractDownloadUrls playlistVideo
        Assert.Equal(result.Length, 2)
        Assert.Equal(expectedUrls.Length, result.Length)
        Assert.Equal(expectedUrls.Head, result.Head)
        Assert.Equal(expectedUrls[1], result[1])

    [<Fact>]
    let ``Generates expected URL for standard playlist`` () =
        let sPlaylist = StandardPlaylist "PLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"
        let expectedUrls = ["https://www.youtube.com/playlist?list=PLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"]
        let result = extractDownloadUrls sPlaylist
        Assert.Equal(result.Length, 1)
        Assert.Equal(expectedUrls.Length, result.Length)
        Assert.Equal(expectedUrls.Head, result.Head)

    [<Fact>]
    let ``Generates expected URL for release playlist`` () =
        let rPlaylist = ReleasePlaylist "OLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"
        let expectedUrls = ["https://www.youtube.com/playlist?list=OLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"]
        let result = extractDownloadUrls rPlaylist
        Assert.Equal(result.Length, 1)
        Assert.Equal(expectedUrls.Length, result.Length)
        Assert.Equal(expectedUrls.Head, result.Head)

    [<Fact>]
    let ``Generates expected URL for channel`` () =
        let channel = Channel "www.youtube.com/channel/UBMmt12UKW571UWtJAgWkWrg"
        let expectedUrls = ["https://www.youtube.com/channel/UBMmt12UKW571UWtJAgWkWrg"]
        let result = extractDownloadUrls channel
        Assert.Equal(result.Length, 1)
        Assert.Equal(expectedUrls.Length, result.Length)
        Assert.Equal(expectedUrls.Head, result.Head)
