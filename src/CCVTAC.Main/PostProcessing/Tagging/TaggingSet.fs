namespace CCVTAC.Main.PostProcessing.Tagging

open CCVTAC.Main
open CCVTAC.Main.IoUtilities
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

    let private extractFilesByType (files: string seq) =
        let hasSupportedAudioExtension (file: string) =
            match Path.GetExtension file with
            | Null -> false
            | NonNull (ext: string) -> Seq.caseInsensitiveContains ext Files.audioFileExtensions

        let jsonFileExt = ".json"
        let imageFileExt = ".jpg"

        let audioFiles = files
                         |> Seq.filter hasSupportedAudioExtension
                         |> Seq.toOption
        let jsonFile   = files |> Seq.tryFind (String.endsWithIgnoreCase jsonFileExt)
        let imageFile  = files |> Seq.tryFind (String.endsWithIgnoreCase imageFileExt)

        (audioFiles, jsonFile, imageFile)

    /// Create a collection of TaggingSets from a collection of file paths related to several video IDs.
    /// Files that don't match the requirements will be ignored.
    let createSets filePaths : TaggingSet list =
        if Seq.isEmpty filePaths then
            []
        else
            // Regex group 0 is the full filename, and group 1 contains the video ID.
            let fileNamesWithVideoIdsRegex =
                Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)", RegexOptions.Compiled)

            filePaths
            |> Seq.map fileNamesWithVideoIdsRegex.Match
            |> Seq.filter _.Success
            |> Seq.map (fun m -> m.Captures |> Seq.cast<Match> |> Seq.head)
            |> Seq.groupBy _.Groups[1].Value
            |> Seq.map (fun (videoId, matches) -> videoId, matches |> Seq.map _.Groups[0].Value)
            |> Seq.choose (fun (videoId, files) ->
                match extractFilesByType files with
                | Some audioFiles, Some jsonFile, Some imageFile ->
                    Some { ResourceId = videoId
                           AudioFilePaths = audioFiles |> Seq.toList
                           JsonFilePath = jsonFile
                           ImageFilePath = imageFile }
                | _ -> None)
            |> List.ofSeq
