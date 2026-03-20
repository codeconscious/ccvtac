namespace CCVTAC.Main.PostProcessing.Tagging

open CCVTAC.Main.IoUtilities
open CCFSharpUtils.Library
open FsToolkit.ErrorHandling
open System.IO
open System.Text.RegularExpressions

/// Contains the names of all files related to tagging audio files.
type TaggingSet =
    private
        { VideoId: string
          AudioFiles: string list
          JsonFile: string
          ImageFile: string }

module TaggingSet =
    // Accessors
    let videoId t    = t.VideoId
    let audioFiles t = t.AudioFiles
    let jsonFile t   = t.JsonFile
    let imageFile t  = t.ImageFile

    let allFiles t =
        t.AudioFiles @ [t.JsonFile; t.ImageFile]

    let private create v a j i =
        { VideoId    = v
          AudioFiles = a |> List.ofSeq
          JsonFile   = j
          ImageFile  = i }

    let private createValidated (videoId, files) : Result<TaggingSet, string list> =
        let ensureNotEmpty xs errorMsg : Validation<'a list, string> =
            if List.isNotEmpty xs
            then Ok xs
            else Error [errorMsg]

        let ensureExactlyOne xs emptyErrorMsg multipleErrorMsg : Validation<'a, string> =
            match xs with
            | []  -> Error [emptyErrorMsg]
            | [x] -> Ok x
            | _   -> Error [multipleErrorMsg]

        let hasSupportedAudioExt (fileName: string) =
            match Path.GetExtension fileName with
            | Null -> false
            | NonNull empty when String.hasNoText empty -> false
            | NonNull ext -> Files.audioFileExts |> List.containsIgnoreCase ext

        let audioFiles = files |> List.filter hasSupportedAudioExt
        let jsonFiles  = files |> Files.filterByExt Files.jsonFileExt
        let imageFiles =
            Files.imageFileExts
            |> List.collect (fun ext -> files |> Files.filterByExt ext)

        Validation.map3
            (fun a j i -> create videoId a j i)
            (ensureNotEmpty audioFiles
                $"No supported audio files found for video ID {videoId}.")
            (ensureExactlyOne jsonFiles
                $"No JSON file found for video ID {videoId}."
                $"Multiple JSON files found for video ID {videoId}.")
            (ensureExactlyOne imageFiles
                $"No image file found for video ID {videoId}."
                $"Multiple image files found for video ID {videoId}.")

    /// Creates a collection of TaggingSets from a collection of file paths related to several video IDs.
    /// Any extra, unnecessary files will be ignored.
    /// Any validation errors will be accumulated and return in an Error.
    let createSets filePaths : Result<TaggingSet list, string list> =
        if Seq.isEmpty filePaths then
            Error ["No file paths to create a tagging set were provided."]
        else
            let isRelevantFile fileName : Match option =
                // Regex group 0 is the full filename, and group 1 contains the video ID.
                let fileNamesHavingVideoIdsRgx =
                    Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)", RegexOptions.Compiled)

                fileName |> Rgx.trySuccessMatch fileNamesHavingVideoIdsRgx

            let fileName (m: Match) = m.Groups[0].Value
            let videoId (m: Match) = m.Groups[1].Value

            filePaths
            |> List.ofSeq
            |> List.choose isRelevantFile
            |> List.groupBy videoId
            |> List.map (List.mapSnds fileName)
            |> List.map createValidated
            |> List.sequenceResultA
            |! List.collect id
