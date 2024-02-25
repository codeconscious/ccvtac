module Tests

open Xunit
open CCVTAC.FSharp.Downloading

module MediaTypeWithIdsTests =
    [<Fact>]
    let ``Correctly detects video URL with its ID`` () =
        let url = "https://www.youtube.com/watch?v=12312312312"
        let expectedId = "12312312312"
        let result = mediaTypeWithIds url
        match result with
        | Ok _ -> function
            | Video id -> Assert.Equal(expectedId, id)
            | _ -> failwith "Incorrect media type"
        | Error e -> failwith $"Unexpected error: {e}"

    [<Fact>]
    let ``Correctly detects playlist video URL with its ID`` () =
        let url = "https://www.youtube.com/watch?v=12312312312&list=OLZK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg&index=1"
        let expectedVideoId = "12312312312"
        let expectedPlaylistId = "OLZK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg"
        let result = mediaTypeWithIds url
        match result with
        | Ok _ -> function
            | PlaylistVideo (v, p) -> Assert.Equal(expectedVideoId, v)
                                      Assert.Equal(expectedPlaylistId, p)
            | _ -> failwith "Incorrect media type"
        | Error e -> failwith $"Unexpected error: {e}"

    [<Fact>]
    let ``Correctly detects standard playlist URL with its ID`` () =
        let url = "https://www.youtube.com/playlist?list=PLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L&index=1"
        let expectedId = "PLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2"
        let result = mediaTypeWithIds url
        match result with
        | Ok _ -> function
            | StandardPlaylist id -> Assert.Equal(expectedId, id)
            | _ -> failwith "Incorrect media type"
        | Error e -> failwith $"Unexpected error: {e}"

    [<Fact>]
    let ``Correctly detects release playlist URL with its ID`` () =
        let url = "https://www.youtube.com/playlist?list=OLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L&index=1"
        let expectedId = "12312312312"
        let result = mediaTypeWithIds url
        match result with
        | Ok _ -> function
            | ReleasePlaylist id -> Assert.Equal(expectedId, id)
            | _ -> failwith "Incorrect media type"
        | Error e -> failwith $"Unexpected error: {e}"

    // TODO: Test for more channel URL types.
    [<Fact>]
    let ``Correctly detects channel URL with its ID`` () =
        let url = "https://www.youtube.com/channel/UBMmt12UKW571UWtJAgWkWrg"
        let expectedId = "UBMmt12UKW571UWtJAgWkWrg"
        let result = mediaTypeWithIds url
        match result with
        | Ok _ -> function
            | Channel id -> Assert.Equal(expectedId, id)
            | _ -> failwith "Incorrect media type"
        | Error e -> failwith $"Unexpected error: {e}"

    [<Fact>]
    let ``Error result when an invalid URL is passed`` () =
        let url = "INVALID URL"
        let result = mediaTypeWithIds url

        let assertion =
            match result with
            | Ok mediaType -> match mediaType with
                              | _ -> false
            | Error _ -> true
        Assert.True(assertion)

module DownloadUrlsTests =
    [<Fact>]
    let ``Generates expected URL for Video`` () =
        let video = Video "12312312312"
        let expectedUrl = ["https://www.youtube.com/watch?v=12312312312"]
        let result = downloadUrls video
        Assert.Equal(expectedUrl.Length, result.Length)
        Assert.Equal(expectedUrl.Head, result.Head)

    [<Fact>]
    let ``Generates expected URL pair for playlist video`` () =
        let playlistVideo = PlaylistVideo ("12312312312", "OLZK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg")
        let videoUrl = "https://www.youtube.com/watch?v=12312312312"
        let playlistUrl = "https://www.youtube.com/playlist?list=OLZK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg"
        let expectedUrls = [videoUrl; playlistUrl]
        let result = downloadUrls playlistVideo
        Assert.Equal(expectedUrls.Length, result.Length)
        Assert.Equal(expectedUrls.Head, result.Head)
        Assert.Equal(expectedUrls.[1], result.[1])

    [<Fact>]
    let ``Generates expected URL for standard playlist`` () =
        let sPlaylist = StandardPlaylist "PLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"
        let expectedUrls = ["https://www.youtube.com/playlist?list=PLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"]
        let result = downloadUrls sPlaylist
        Assert.Equal(expectedUrls.Length, result.Length)
        Assert.Equal(expectedUrls.Head, result.Head)

    [<Fact>]
    let ``Generates expected URL for release playlist`` () =
        let rPlaylist = ReleasePlaylist "OLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"
        let expectedUrls = ["https://www.youtube.com/playlist?list=OLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"]
        let result = downloadUrls rPlaylist
        Assert.Equal(expectedUrls.Length, result.Length)
        Assert.Equal(expectedUrls.Head, result.Head)

    [<Fact>]
    let ``Generates expected URL for channel`` () =
        let channel = Channel "www.youtube.com/channel/UBMmt12UKW571UWtJAgWkWrg"
        let expectedUrls = ["https://www.youtube.com/channel/UBMmt12UKW571UWtJAgWkWrg"]
        let result = downloadUrls channel
        Assert.Equal(expectedUrls.Length, result.Length)
        Assert.Equal(expectedUrls.Head, result.Head)

    // [<Fact>]
    // let ``Fails when passed invalid URL`` () =
    //     let url = "not a valid URL"
    //     let result = downloadUrls url
    //     Assert.True(result.IsNone)
