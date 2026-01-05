namespace CCVTAC.Main.PostProcessing.Tagging

open CCVTAC.Main
open CCVTAC.Main.IoUtilities
open FsToolkit.ErrorHandling
open System.IO
open System.Text.RegularExpressions

/// Contains the names of all files related to tagging audio files.
type TaggingSet =
    private
        { VideoId: string
          AudioFilePaths: string list
          JsonFilePath: string
          ImageFilePath: string }

module TaggingSet =
    // Accessors
    let videoId ts        = ts.VideoId
    let audioFilePaths ts = ts.AudioFilePaths
    let jsonFilePath ts   = ts.JsonFilePath
    let imageFilePath ts  = ts.ImageFilePath

    let allFiles ts =
        ts.AudioFilePaths @ [ts.JsonFilePath; ts.ImageFilePath]

    let private create (videoId, files) =
        let ensureNotEmpty (xs: 'a list) errorMsg : Validation<'a list, string list> =
            if List.isNotEmpty xs
            then Ok xs
            else Error [[errorMsg]]

        let ensureExactlyOne (xs: 'a list) emptyErrorMsg multipleErrorMsg : Validation<'a, string list> =
            match xs with
            | []  -> Error [[emptyErrorMsg]]
            | [x] -> Ok x
            | _   -> Error [[multipleErrorMsg]]

        let hasSupportedAudioExt (fileName: string) =
            match Path.GetExtension fileName with
            | Null -> false
            | NonNull (ext: string) -> Seq.caseInsensitiveContains ext Files.audioFileExtensions

        let files' = List.ofSeq files

        let audioFiles = files' |> List.filter hasSupportedAudioExt
        let jsonFiles  = files' |> List.filter (String.endsWithIgnoreCase ".json")
        let imageFiles = [".jpg"; ".jpeg"] |> List.collect (fun ext -> files' |> List.filter (String.endsWithIgnoreCase ext))

        Validation.map3
            (fun a j i -> a, j, i)
            (ensureNotEmpty   audioFiles $"No supported audio files found for video ID {videoId}.")
            (ensureExactlyOne jsonFiles  $"No JSON file found for video ID {videoId}."  $"Multiple JSON files found for video ID {videoId}.")
            (ensureExactlyOne imageFiles $"No image file found for video ID {videoId}." $"Multiple image files found for video ID {videoId}.")
        |> function
        | Ok (a, j, i) ->
            Ok { VideoId = videoId
                 AudioFilePaths = a |> List.ofSeq
                 JsonFilePath = j
                 ImageFilePath = i }
        | Error msgs -> Error (msgs |> List.collect id)

    /// Creates a collection of TaggingSets from a collection of file paths related to several video IDs.
    /// Files that don't match the requirements will be ignored.
    let createSets filePaths : Result<TaggingSet list, string list> =
        if Seq.isEmpty filePaths then
            Error ["No filepaths to create a tagging set were provided."]
        else
            // Regex group 0 is the full filename, and group 1 contains the video ID.
            let fileNamesWithVideoIdsRegex =
                Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)", RegexOptions.Compiled)

            filePaths
            |> List.ofSeq
            |> List.map fileNamesWithVideoIdsRegex.Match
            |> List.filter _.Success
            |> List.map (fun m -> m.Captures |> Seq.cast<Match> |> Seq.head)
            |> List.groupBy _.Groups[1].Value // By video ID
            |> List.map (fun (videoId, matches) -> videoId, matches |> List.map _.Groups[0].Value)
            |> List.map create
            |> List.sequenceResultA
            |> Result.mapError (List.collect id)
