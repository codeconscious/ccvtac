module TaggingSetTests

open CCVTAC.Main.PostProcessing.Tagging
open CCVTAC.Main.PostProcessing.Tagging.TaggingSets
open System
open System.IO
open System.Text
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

        let files = [audioFile; jsonFile; imageFile]

        let expected : Result<TaggingSet list, string list list> =
          Ok [
              {
                ResourceId = videoId
                AudioFilePaths = [audioFile]
                JsonFilePath = jsonFile
                ImageFilePath = imageFile
              }
          ]

        let actual = createSets files

        Assert.Equal(expected, actual)
