namespace CCVTAC.Console.PostProcessing.Tagging

open CCVTAC.Console
open CCVTAC.Console.IoUtilities
open System.IO
open System.Text.RegularExpressions

/// Contains all the data necessary for tagging a related set of files.
module TaggingSets =

    type TaggingSet =
        { ResourceId: string
          AudioFilePaths: string list
          JsonFilePath: string
          ImageFilePath: string }

    let allFiles taggingSet =
        List.concat [taggingSet.AudioFilePaths; [taggingSet.JsonFilePath; taggingSet.ImageFilePath]]

    /// Create a collection of TaggingSets from a collection of file paths related to several video IDs.
    /// Files that don't match the requirements will be ignored.
    let createSets filePaths : TaggingSet list =
        if Seq.isEmpty filePaths then
            []
        else
            let jsonFileExt = ".json"
            let imageFileExt = ".jpg"

            // Regex: group 1 holds the video id; group 0 is the full filename
            let fileNamesWithVideoIdsRegex =
                Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)", RegexOptions.Compiled)

            let fileHasSupportedExtension (file: string) =
                match Path.GetExtension file with
                | Null -> false
                | NonNull (ext: string) -> Seq.caseInsensitiveContains ext FileIo.audioFileExtensions

            filePaths
            |> Seq.map fileNamesWithVideoIdsRegex.Match
            |> Seq.filter _.Success
            |> Seq.map (fun m -> m.Captures |> Seq.cast<Match> |> Seq.head)
            |> Seq.groupBy _.Groups[1].Value
            |> Seq.map (fun (videoId, matches) -> videoId, matches |> Seq.map _.Groups[0].Value)
            |> Seq.filter (fun (_, files) ->
                let isSupportedExt = files |> Seq.exists fileHasSupportedExtension
                let jsonCount  = files |> Seq.filter (String.endsWithIgnoringCase jsonFileExt)  |> Seq.length
                let imageCount = files |> Seq.filter (String.endsWithIgnoringCase imageFileExt) |> Seq.length
                isSupportedExt
                    && Numerics.isOne jsonCount
                    && Numerics.isOne imageCount)
            |> Seq.map (fun (videoId, files) ->
                let audioFiles = files |> Seq.filter fileHasSupportedExtension
                let jsonFile   = files |> Seq.find (String.endsWithIgnoringCase jsonFileExt)
                let imageFile  = files |> Seq.find (String.endsWithIgnoringCase imageFileExt)
                { ResourceId = videoId
                  AudioFilePaths = audioFiles |> Seq.toList
                  JsonFilePath = jsonFile
                  ImageFilePath = imageFile })
            |> List.ofSeq
