module TaggingSetTests

open CCVTAC.Main.PostProcessing.Tagging.TaggingSets
open System.IO
open Xunit

module TaggingSetInstantiationTests =

    [<Fact>]
    let ``error when no files are provided`` () =
        let expected = Error [["No filepaths to create a tagging set were provided."]]
        let actual = createSets []
        Assert.Equal(expected, actual)

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
            Error [[$"No supported audio files were found for video ID {videoId}."]]

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
          Error [[$"No JSON file was found for video ID {videoId}."]]

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
          Error [[$"No image file was found for video ID {videoId}."]]

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
          Error [[$"No JSON file was found for video ID {videoId}."
                  $"No image file was found for video ID {videoId}."]]

        let actual = createSets [audioFile]

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
        ]
        let actual = createSets files

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``fails with multiple errors when multiple files from playlists are missing their metadata files`` () =
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")

        // Valid, so is not included in the expected error output.
        let videoId1 = "__FuYW30t7E"
        let fileNameBase1 = $"""{Path.Combine(dir, "Video Name 1")} [{videoId1}]"""
        let audioFile1 = $"{fileNameBase1}.m4a"
        let jsonFile1 = $"{fileNameBase1}.info.json"
        let imageFile1 = $"{fileNameBase1}.jpg"

        // Missing audio file.
        let videoId2 = "G_d242400EF"
        let fileNameBase2 = $"""{Path.Combine(dir, "Video Name 2")} [{videoId2}]"""
        let jsonFile2 = $"{fileNameBase2}.info.json"
        let imageFile2 = $"{fileNameBase2}.jpg"

        // Missing JSON file.
        let videoId3 = "048_702850G"
        let fileNameBase3 = $"""{Path.Combine(dir, "Video Name 3")} [{videoId3}]"""
        let audioFile3 = $"{fileNameBase3}.m4a"
        let imageFile3 = $"{fileNameBase3}.jpg"

        // Missing image file.
        let videoId4 = "048_702850H"
        let fileNameBase4 = $"""{Path.Combine(dir, "Video Name 4")} [{videoId4}]"""
        let audioFile4 = $"{fileNameBase4}.m4a"
        let jsonFile4 = $"{fileNameBase4}.info.json"

        // Missing audio and JSON file.
        let videoId5 = "048_702850I"
        let fileNameBase5 = $"""{Path.Combine(dir, "Video Name 5")} [{videoId5}]"""
        let imageFile5 = $"{fileNameBase5}.jpg"

        // Missing audio and image file.
        let videoId6 = "048_702850J"
        let fileNameBase6 = $"""{Path.Combine(dir, "Video Name 6")} [{videoId6}]"""
        let jsonFile6 = $"{fileNameBase6}.info.json"

        let expected : Result<TaggingSet list, string list list> =
            Error
              [[$"No supported audio files were found for video ID {videoId2}."]
               [$"No JSON file was found for video ID {videoId3}."]
               [$"No image file was found for video ID {videoId4}."]
               [$"No supported audio files were found for video ID {videoId5}."
                $"No JSON file was found for video ID {videoId5}."]
               [$"No supported audio files were found for video ID {videoId6}."
                $"No image file was found for video ID {videoId6}."]]

        let files = [
            audioFile1; jsonFile1; imageFile1
            jsonFile2; imageFile2
            audioFile3; imageFile3
            audioFile4; jsonFile4
            imageFile5
            jsonFile6
        ]

        let actual = createSets files

        Assert.Equal(expected, actual)


    [<Fact>]
    let ``parses split video files`` () =
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")

        let videoId = "__FuYW30t7E"
        let audioFiles =
            List.mapi
                (fun i _ -> $"""{Path.Combine(dir, $"Video Name - 00{i} Name of Chapter {i}")} [{videoId}].m4a""")
                [1..9]
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

        let files = audioFiles @ [jsonFile; imageFile ]
        let actual = createSets files

        Assert.Equal(expected, actual)
