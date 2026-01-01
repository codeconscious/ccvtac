module TaggingSetTests

open CCVTAC.Main.PostProcessing.Tagging.TaggingSets
open System.IO
open Xunit

module TaggingSetInstantiationTests =

    [<Fact>]
    let ``parses video files`` () =
        let videoId = "__FuYW30t7E"
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")
        let fileNameBase = $"""{Path.Combine(dir, "Video Name")} [{videoId}]"""
        let audioFile = $"{fileNameBase}.m4a"
        let jsonFile = $"{fileNameBase}.info.json"
        let imageFile = $"{fileNameBase}.jpg"

        let expected : Result<TaggingSet list, string list list> =
          Ok [
              {
                ResourceId = videoId
                AudioFilePaths = [audioFile]
                JsonFilePath = jsonFile
                ImageFilePath = imageFile
              }
          ]

        let files = [audioFile; jsonFile; imageFile]
        let actual = createSets files

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``fails for video files when audio file is missing`` () =
        let videoId = "__FuYW30t7E"
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")
        let fileNameBase = $"""{Path.Combine(dir, "Video Name")} [{videoId}]"""
        let jsonFile = $"{fileNameBase}.info.json"
        let imageFile = $"{fileNameBase}.jpg"

        let expected : Result<TaggingSet list, string list list> =
            Error [["No supported audio files were found by extension."]]

        let files = [jsonFile; imageFile]
        let actual = createSets files

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``fails for video files when JSON is missing`` () =
        let videoId = "__FuYW30t7E"
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")
        let fileNameBase = $"""{Path.Combine(dir, "Video Name")} [{videoId}]"""
        let audioFile = $"{fileNameBase}.m4a"
        let imageFile = $"{fileNameBase}.jpg"

        let expected : Result<TaggingSet list, string list list> =
          Error [["No JSON file was found."]]

        let files = [audioFile; imageFile]
        let actual = createSets files

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``fails for video files when image file is missing`` () =
        let videoId = "__FuYW30t7E"
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")
        let fileNameBase = $"""{Path.Combine(dir, "Video Name")} [{videoId}]"""
        let audioFile = $"{fileNameBase}.m4a"
        let jsonFile = $"{fileNameBase}.json"

        let expected : Result<TaggingSet list, string list list> =
          Error [["No image file was found."]]

        let files = [audioFile; jsonFile]
        let actual = createSets files

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``fails for video files when JSON and image files are missing`` () =
        let videoId = "__FuYW30t7E"
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")
        let fileNameBase = $"""{Path.Combine(dir, "Video Name")} [{videoId}]"""
        let audioFile = $"{fileNameBase}.m4a"

        let expected : Result<TaggingSet list, string list list> =
          Error [["No JSON file was found."; "No image file was found."]]

        let files = [audioFile]
        let actual = createSets files

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``parses multiple files from playlist`` () =
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")

        let videoId1 = "__FuYW30t7E"
        let fileNameBase1 = $"""{Path.Combine(dir, "Video Name 1")} [{videoId1}]"""
        let audioFile1 = $"{fileNameBase1}.m4a"
        let jsonFile1 = $"{fileNameBase1}.info.json"
        let imageFile1 = $"{fileNameBase1}.jpg"

        let videoId2 = "G_d242400EF"
        let fileNameBase2 = $"""{Path.Combine(dir, "Video Name 2")} [{videoId2}]"""
        let audioFile2 = $"{fileNameBase2}.m4a"
        let jsonFile2 = $"{fileNameBase2}.info.json"
        let imageFile2 = $"{fileNameBase2}.jpg"

        let videoId3 = "048_702850G"
        let fileNameBase3 = $"""{Path.Combine(dir, "Video Name 2")} [{videoId3}]"""
        let audioFile3 = $"{fileNameBase3}.m4a"
        let jsonFile3 = $"{fileNameBase3}.info.json"
        let imageFile3 = $"{fileNameBase3}.jpg"

        let playlistId = "OLAK5uy_ljoU26IjmfmI__eBcG_r0GzH-K3GaJy3s"
        let playlistNameBase = $"""{Path.Combine(dir, "Playlist Name")} [{playlistId}]"""
        let playlistJsonFile = $"{playlistNameBase}.info.json"
        let playlistImageFile = $"{playlistNameBase}.jpg"

        let expected : Result<TaggingSet list, string list list> =
          Ok [
              {
                ResourceId = videoId1
                AudioFilePaths = [audioFile1]
                JsonFilePath = jsonFile1
                ImageFilePath = imageFile1
              }
              {
                ResourceId = videoId2
                AudioFilePaths = [audioFile2]
                JsonFilePath = jsonFile2
                ImageFilePath = imageFile2
              }
              {
                ResourceId = videoId3
                AudioFilePaths = [audioFile3]
                JsonFilePath = jsonFile3
                ImageFilePath = imageFile3
              }
          ]

        let files = [
            audioFile1; jsonFile1; imageFile1
            audioFile2; jsonFile2; imageFile2
            audioFile3; jsonFile3; imageFile3
            playlistJsonFile; playlistImageFile
        ]
        let actual = createSets files

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``parses split video files`` () =
        (*
            How to Use YouTube Chapters - YouTube Video Sections - 001 Introduction [6-0cERIVsFg].m4a
            How to Use YouTube Chapters - YouTube Video Sections - 002 How to Add Chapters to a YouTube Video [6-0cERIVsFg].m4a
            How to Use YouTube Chapters - YouTube Video Sections - 003 How to Use Chapters as a Viewer [6-0cERIVsFg].m4a
            How to Use YouTube Chapters - YouTube Video Sections [6-0cERIVsFg].info.json
            How to Use YouTube Chapters - YouTube Video Sections [6-0cERIVsFg].jpg
        *)
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")

        let videoId = "__FuYW30t7E"
        let audioFiles =
            [1..9]
            |> List.mapi (fun i x -> $"""{Path.Combine(dir, $"Video Name - 00{i} Name of Chapter {i}")} [{videoId}].m4a""")
        let fileNameBase = $"""{Path.Combine(dir, "Video Name")} [{videoId}]"""
        let jsonFile = $"{fileNameBase}.info.json"
        let imageFile = $"{fileNameBase}.jpg"


        let expected : Result<TaggingSet list, string list list> =
          Ok [
              {
                ResourceId = videoId
                AudioFilePaths = audioFiles
                JsonFilePath = jsonFile
                ImageFilePath = imageFile
              }
          ]

        let files =  audioFiles @ [jsonFile; imageFile ]
        let actual = createSets files

        Assert.Equal(expected, actual)
