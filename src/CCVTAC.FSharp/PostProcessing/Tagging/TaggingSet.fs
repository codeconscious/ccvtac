namespace CCVTAC.Console.PostProcessing.Tagging

open System
open System.IO
open System.Text.RegularExpressions
open System.Collections.Generic
open System.Collections.Immutable
open CCVTAC.Console

/// Contains all the data necessary for tagging a related set of files.
[<Struct>]
type TaggingSet =
    { ResourceId: string
      AudioFilePaths: string list
      JsonFilePath: string
      ImageFilePath: string }
    /// Expose all related files as a read-only list
    member this.AllFiles : IReadOnlyList<string> =
        // combine the immutable hash set with json and image paths preserving as a list
        let audio = this.AudioFilePaths |> Seq.toList
        List.concat [ audio; [ this.JsonFilePath; this.ImageFilePath ] ] :> IReadOnlyList<string>

    // Private constructor helper to perform validation (not directly callable from outside)
    static member private CreateValidated(resourceId: string, audioFilePaths: ICollection<string>, jsonFilePath: string, imageFilePath: string) =
        if hasNoText resourceId then
            invalidArg "resourceId" "The resource ID must be provided."
        if hasNoText jsonFilePath then
            invalidArg "jsonFilePath" "The JSON file path must be provided."
        if hasNoText imageFilePath then
            invalidArg "imageFilePath" "The image file path must be provided."
        if audioFilePaths.Count = 0 then
            invalidArg "audioFilePaths" "At least one audio file path must be provided."

        let resourceIdTrimmed = resourceId.Trim()
        let jsonTrimmed = jsonFilePath.Trim()
        let imageTrimmed = imageFilePath.Trim()
        let audioSet = ImmutableHashSet.CreateRange(StringComparer.OrdinalIgnoreCase, audioFilePaths)
        { ResourceId = resourceIdTrimmed
          AudioFilePaths = audioSet |> Seq.toList
          JsonFilePath = jsonTrimmed
          ImageFilePath = imageTrimmed }

    /// Create a collection of TaggingSets from a collection of file paths related to several video IDs.
    /// Files that don't match the requirements will be ignored.
    // static member CreateSets (filePaths: ICollection<string>) : TaggingSet list =
    static member CreateSets (filePaths: ICollection<string>) : TaggingSet list =
        if Seq.isEmpty filePaths then
            []
        else
            let jsonFileExt = ".json"
            let imageFileExt = ".jpg"

            // Regex: group 1 holds the video id; group 0 is the full filename
            let fileNamesWithVideoIdsRegex =
                Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)", RegexOptions.Compiled)

            filePaths
            |> Seq.map fileNamesWithVideoIdsRegex.Match
            |> Seq.filter _.Success
            |> Seq.map (fun m -> m.Captures |> Seq.cast<Match> |> Seq.head)
            |> Seq.groupBy _.Groups[1].Value
            |> Seq.map (fun (videoId, matches) -> videoId, matches |> Seq.map _.Groups[0].Value)
            |> Seq.filter (fun (_, files) ->
                let filesSeq = files |> Seq.toArray
                let isSupportedExtension =
                    filesSeq
                    |> Seq.exists (fun f ->
                        let f' = match Path.GetExtension (f: string) with Null -> "" | NonNull (x: string) -> x // TODO: Improve.
                        caseInsensitiveContains AudioExtensions f')
                let jsonCount =
                    filesSeq |> Seq.filter (fun f -> f.EndsWith(jsonFileExt, StringComparison.OrdinalIgnoreCase)) |> Seq.length
                let imageCount =
                    filesSeq |> Seq.filter (fun f -> f.EndsWith(imageFileExt, StringComparison.OrdinalIgnoreCase)) |> Seq.length
                isSupportedExtension && jsonCount = 1 && imageCount = 1)
            |> Seq.map (fun (key, files) ->
                let filesArr = files |> Seq.toArray
                let audioFiles =
                    filesArr
                    |> Seq.filter (fun f ->
                        let f' = match Path.GetExtension (f: string) with Null -> "" | NonNull (x: string) -> x // TODO: Improve.
                        caseInsensitiveContains AudioExtensions f')
                    |> Seq.toList
                let jsonFile = filesArr |> Seq.find (fun f -> f.EndsWith(jsonFileExt, StringComparison.OrdinalIgnoreCase))
                let imageFile = filesArr |> Seq.find (fun f -> f.EndsWith(imageFileExt, StringComparison.OrdinalIgnoreCase))

                { ResourceId = key
                  AudioFilePaths = audioFiles
                  JsonFilePath = jsonFile
                  ImageFilePath = imageFile })
            |> List.ofSeq
