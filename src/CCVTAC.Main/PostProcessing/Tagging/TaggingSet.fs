namespace CCVTAC.Main.PostProcessing.Tagging

open CCVTAC.Main
open CCVTAC.Main.IoUtilities
open FsToolkit.ErrorHandling
open System.IO
open System.Text.RegularExpressions

/// Contains all the data necessary to tag audio files.
module TaggingSets = // TODO: Perform instantiation in a more idiomatic way.

    type TaggingSet =
        { ResourceId: string
          AudioFilePaths: string list
          JsonFilePath: string
          ImageFilePath: string }

    let allFiles taggingSet =
        List.concat [taggingSet.AudioFilePaths; [taggingSet.JsonFilePath; taggingSet.ImageFilePath]]

    let private extractFilesByType videoId (files: string seq) =
        let validateNonEmpty (xs: 'a list) errorMsg : Validation<unit, string list> =
            if List.isNotEmpty xs then Ok () else Error [[errorMsg]]

        let validateSome (x: 'a option) errorMsg : Validation<unit, string list> =
            if x.IsSome then Ok () else Error [[errorMsg]]

        let validateExactlyOne (xs: 'a list) noneErrorMsg multipleErrorMsg : Validation<unit,string list> =
            if List.isEmpty xs
            then Error [[noneErrorMsg]]
            elif List.hasMultiple xs
            then Error [[multipleErrorMsg]]
            else Ok ()

        let hasSupportedAudioExtension (file: string) =
            match Path.GetExtension file with
            | Null -> false
            | NonNull (ext: string) -> Seq.caseInsensitiveContains ext Files.audioFileExtensions

        let jsonFileExt = ".json"
        let imageFileExts = [".jpg"; ".jpeg"]

        let files' = files |> List.ofSeq
        let audioFiles = files' |> List.filter hasSupportedAudioExtension
        let jsonFile   = files' |> List.tryFind (String.endsWithIgnoreCase jsonFileExt)
        let imageFiles =
            imageFileExts
            |> List.map (fun i -> files' |> List.tryFind (String.endsWithIgnoreCase i))
            |> List.choose id

        Validation.map3 (fun _ _ _ -> audioFiles, jsonFile, imageFiles[0])
            (validateNonEmpty audioFiles
                 $"No supported audio files were found for video ID {videoId}.")
            (validateSome jsonFile
                 $"No JSON file was found for video ID {videoId}.")
            (validateExactlyOne imageFiles
                 $"No image file was found for video ID {videoId}."
                 $"Multiple image files were found for video ID {videoId}.")

    /// Create a collection of TaggingSets from a collection of file paths related to several video IDs.
    /// Files that don't match the requirements will be ignored.
    let createSets filePaths : Result<TaggingSet list, string list list> =
        if Seq.isEmpty filePaths then
            Error [["No filepaths to create a tagging set were provided."]]
        else
            // Regex group 0 is the full filename, and group 1 contains the video ID.
            let fileNamesWithVideoIdsRegex =
                Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)", RegexOptions.Compiled)

            filePaths
            |> List.ofSeq
            |> List.map fileNamesWithVideoIdsRegex.Match
            |> List.filter _.Success
            |> List.map (fun m -> m.Captures |> Seq.cast<Match> |> Seq.head)
            |> List.groupBy _.Groups[1].Value
            |> List.map (fun (videoId, matches) -> videoId, matches |> List.map _.Groups[0].Value)
            |> List.map (fun (videoId, files) ->
                match extractFilesByType videoId files with
                | Ok (audioFiles, Some jsonFile, imageFile) ->
                     Ok { ResourceId = videoId
                          AudioFilePaths = audioFiles |> Seq.toList
                          JsonFilePath = jsonFile
                          ImageFilePath = imageFile }
                | Ok _ -> Error [$"Something went wrong creating a tagging set using the files for video ID %s{videoId}."]
                | Error msgs -> Error (msgs |> List.collect id))
            |> List.sequenceResultA
