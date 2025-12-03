namespace CCVTAC.Console.PostProcessing.Tagging

open CCVTAC.Console
open System.IO
open System.Text.RegularExpressions

/// Contains all the data necessary for tagging a related set of files.
module TaggingSets =

    [<Struct>]
    type TaggingSet =
        { ResourceId: string
          AudioFilePaths: string list
          JsonFilePath: string
          ImageFilePath: string }

    let allFiles taggingSet =
        List.concat [taggingSet.AudioFilePaths; [taggingSet.JsonFilePath; taggingSet.ImageFilePath]]

    /// Create a collection of TaggingSets from a collection of file paths related to several video IDs.
    /// Files that don't match the requirements will be ignored.
    let createSets (filePaths: string seq) : TaggingSet list =
        if Seq.isEmpty filePaths then
            []
        else
            let jsonFileExt = ".json"
            let imageFileExt = ".jpg"

            // Regex: group 1 holds the video id; group 0 is the full filename
            let fileNamesWithVideoIdsRegex =
                Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)", RegexOptions.Compiled)

            let fileHasSupportedExtension (f: string) =
                match Path.GetExtension f with
                | Null -> false
                | NonNull (x: string) -> Seq.caseInsensitiveContains x AudioExtensions

            filePaths
            |> Seq.map fileNamesWithVideoIdsRegex.Match
            |> Seq.filter _.Success
            |> Seq.map (fun m -> m.Captures |> Seq.cast<Match> |> Seq.head)
            |> Seq.groupBy _.Groups[1].Value
            |> Seq.map (fun (videoId, matches) -> videoId, matches |> Seq.map _.Groups[0].Value)
            |> Seq.filter (fun (_, fileNames) ->
                let isSupportedExtension = fileNames |> Seq.exists fileHasSupportedExtension
                let jsonCount = fileNames |> Seq.filter (String.endsWithIgnoringCase jsonFileExt) |> Seq.length
                let imageCount = fileNames |> Seq.filter (String.endsWithIgnoringCase imageFileExt) |> Seq.length
                isSupportedExtension && jsonCount = 1 && imageCount = 1)
            |> Seq.map (fun (key, files) ->
                let audioFiles = files |> Seq.filter fileHasSupportedExtension
                let jsonFile = files |> Seq.find (String.endsWithIgnoringCase jsonFileExt)
                let imageFile = files |> Seq.find (String.endsWithIgnoringCase imageFileExt)
                { ResourceId = key
                  AudioFilePaths = audioFiles |> Seq.toList
                  JsonFilePath = jsonFile
                  ImageFilePath = imageFile })
            |> List.ofSeq
