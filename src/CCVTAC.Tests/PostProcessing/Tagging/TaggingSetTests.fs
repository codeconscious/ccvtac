module TaggingSetTests

open CCVTAC.Main.PostProcessing.Tagging.TaggingSet
open CCVTAC.Main.PostProcessing.Tagging
open System.IO
open Xunit

module TaggingSetInstantiationTests =

    [<Fact>]
    let ``error when no files are provided`` () =
        let expected = Error ["No filepaths to create a tagging set were provided."]
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

        let files = [audioFile; jsonFile; imageFile]
        let actual = createSets files

        match actual with
        | Ok ts ->
            Assert.Equal(videoId, TaggingSet.videoId ts[0])
            Assert.Equal<string list>([audioFile], TaggingSet.audioFilePaths ts[0])
            Assert.Equal(jsonFile, TaggingSet.jsonFilePath ts[0])
            Assert.Equal(imageFile, TaggingSet.imageFilePath ts[0])
        | Error errs -> failwith $"%A{errs}"

    [<Fact>]
    let ``fails for video files when audio file is missing`` () =
        let videoId = "__FuYW30t7E"
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")
        let fileNameBase = $"""{Path.Combine(dir, "Video Name")} [{videoId}]"""
        let jsonFile = $"{fileNameBase}.info.json"
        let imageFile = $"{fileNameBase}.jpg"

        let expected = Error [$"No supported audio files found for video ID {videoId}."]

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

        let expected = Error [$"No JSON file found for video ID {videoId}."]

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

        let expected = Error [$"No image file found for video ID {videoId}."]

        let files = [audioFile; jsonFile]
        let actual = createSets files

        Assert.Equal(expected, actual)

    [<Fact>]
    let ``fails for video files when JSON and image files are missing`` () =
        let videoId = "__FuYW30t7E"
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")
        let fileNameBase = $"""{Path.Combine(dir, "Video Name")} [{videoId}]"""
        let audioFile = $"{fileNameBase}.m4a"

        let expected =
          Error [$"No JSON file found for video ID {videoId}."
                 $"No image file found for video ID {videoId}."]

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

        let files = [
            audioFile1; jsonFile1; imageFile1
            audioFile2; jsonFile2; imageFile2
            audioFile3; jsonFile3; imageFile3
        ]

        let actual = createSets files

        match actual with
        | Ok ts ->
            // Set 1
            Assert.Equal(videoId1, TaggingSet.videoId ts[0])
            Assert.Equal<string list>([audioFile1], TaggingSet.audioFilePaths ts[0])
            Assert.Equal(jsonFile1, TaggingSet.jsonFilePath ts[0])
            Assert.Equal(imageFile1, TaggingSet.imageFilePath ts[0])

            // Set 2
            Assert.Equal(videoId2, TaggingSet.videoId ts[1])
            Assert.Equal<string list>([audioFile2], TaggingSet.audioFilePaths ts[1])
            Assert.Equal(jsonFile2, TaggingSet.jsonFilePath ts[1])
            Assert.Equal(imageFile2, TaggingSet.imageFilePath ts[1])

            // Set 3
            Assert.Equal(videoId3, TaggingSet.videoId ts[2])
            Assert.Equal<string list>([audioFile3], TaggingSet.audioFilePaths ts[2])
            Assert.Equal(jsonFile3, TaggingSet.jsonFilePath ts[2])
            Assert.Equal(imageFile3, TaggingSet.imageFilePath ts[2])
        | Error errs -> failwith $"%A{errs}"

    [<Fact>]
    let ``fails when multiple JSON files are found`` () =
        let videoId = "__FuYW30t7E"
        let dir = Path.Combine("user", "Downloads", "Audio", "tmp")
        let fileNameBase = $"""{Path.Combine(dir, "Video Name")} [{videoId}]"""
        let audioFile = $"{fileNameBase}.m4a"
        let jsonFile1 = $"{fileNameBase}.info.json"
        let jsonFile2 = $"{fileNameBase}_2.info.json"
        let imageFile = $"{fileNameBase}.jpg"

        let expected = Error [$"Multiple JSON files found for video ID {videoId}."]

        let files = [audioFile; jsonFile1; jsonFile2; imageFile]
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

        let expected =
            Error
                [ $"No supported audio files found for video ID {videoId2}."
                  $"No JSON file found for video ID {videoId3}."
                  $"No image file found for video ID {videoId4}."
                  $"No supported audio files found for video ID {videoId5}."
                  $"No JSON file found for video ID {videoId5}."
                  $"No supported audio files found for video ID {videoId6}."
                  $"No image file found for video ID {videoId6}." ]

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

        let files = audioFiles @ [jsonFile; imageFile ]
        let actual = createSets files

        match actual with
        | Ok ts ->
            Assert.Equal(videoId, TaggingSet.videoId ts[0])
            Assert.Equal<string list>(audioFiles, TaggingSet.audioFilePaths ts[0])
            Assert.Equal(jsonFile, TaggingSet.jsonFilePath ts[0])
            Assert.Equal(imageFile, TaggingSet.imageFilePath ts[0])
        | Error errs -> failwith $"%A{errs}"
