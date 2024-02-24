module Tests

open Xunit
open CCVTAC.FSharp.YouTube

[<Fact>]
let ``Correctly detects video URL and its ID`` () =
    let url = "https://www.youtube.com/watch?v=12312312312"
    let result = generateDownloadUrls url
    match result with
        | Some s ->
            Assert.Equal(Video, (fst s))
            Assert.Equal(url, (snd s).Head)
        | None -> failwith "Unexpectedly failed!"

[<Fact>]
let ``Correctly detects playlist video URL and its IDs`` () =
    let url = "https://www.youtube.com/watch?v=12312312312&list=OLZK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg&index=1"
    let expectedVideoUrl = "https://www.youtube.com/watch?v=12312312312"
    let expectedPlaylistUrl = "https://www.youtube.com/playlist?list=OLZK5uy_kgsbf_bzaknqCjNbb2BtnfylIvHdNlKzg"
    let result = generateDownloadUrls url
    match result with
        | Some s ->
            Assert.Equal(PlaylistVideo, (fst s))
            Assert.Equal(expectedVideoUrl, (snd s).Head)
            Assert.Equal(expectedPlaylistUrl, (snd s).[1])
        | None -> failwith "Unexpectedly failed!"

[<Fact>]
let ``Correctly detects standard playlist URL and its ID`` () =
    let url = "https://www.youtube.com/playlist?list=PLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"
    let result = generateDownloadUrls url
    match result with
        | Some s ->
            Assert.Equal(StandardPlaylist, (fst s))
            Assert.Equal(url, (snd s).Head)
        | None -> failwith "Unexpectedly failed!"

[<Fact>]
let ``Correctly detects release playlist URL and its ID`` () =
    let url = "https://www.youtube.com/playlist?list=OLaB53ktYgG5CBaIe-otRu41Wop8Ji8C2L"
    let result = generateDownloadUrls url
    match result with
        | Some s ->
            Assert.Equal(ReleasePlaylist, (fst s))
            Assert.Equal(url, (snd s).Head)
        | None -> failwith "Unexpectedly failed!"

[<Fact>]
let ``Correctly detects channel URL and its ID`` () =
    let url = "https://www.youtube.com/channel/UBMmt12UKW571UWtJAgWkWrg"
    let result = generateDownloadUrls url
    match result with
        | Some s ->
            Assert.Equal(Channel, (fst s))
            Assert.Equal(url, (snd s).Head)
        | None -> failwith "Unexpectedly failed!"

[<Fact>]
let ``Fails when passed invalid URL`` () =
    let url = "not a valid URL"
    let result = generateDownloadUrls url
    Assert.True(result.IsNone)
