namespace CCVTAC.Console.PostProcessing.Tagging

open System
open System.IO
open System.Text.RegularExpressions
open System.Collections.Generic
open System.Collections.Immutable

/// Contains all the data necessary for tagging a related set of files.
[<Struct>]
type TaggingSet =
    { ResourceId: string
      AudioFilePaths: ImmutableHashSet<string>
      JsonFilePath: string
      ImageFilePath: string }
    /// Expose all related files as a read-only list
    member this.AllFiles : IReadOnlyList<string> =
        // combine the immutable hash set with json and image paths preserving as a list
        let audio = this.AudioFilePaths |> Seq.toList
        List.concat [ audio; [ this.JsonFilePath; this.ImageFilePath ] ] :> IReadOnlyList<string>

    // Private constructor helper to perform validation (not directly callable from outside)
    static member private CreateValidated(resourceId: string, audioFilePaths: ICollection<string>, jsonFilePath: string, imageFilePath: string) =
        if String.IsNullOrWhiteSpace resourceId then
            invalidArg "resourceId" "The resource ID must be provided."
        if String.IsNullOrWhiteSpace jsonFilePath then
            invalidArg "jsonFilePath" "The JSON file path must be provided."
        if String.IsNullOrWhiteSpace imageFilePath then
            invalidArg "imageFilePath" "The image file path must be provided."
        if audioFilePaths.Count = 0 then
            invalidArg "audioFilePaths" "At least one audio file path must be provided."

        let resourceIdTrimmed = resourceId.Trim()
        let jsonTrimmed = jsonFilePath.Trim()
        let imageTrimmed = imageFilePath.Trim()
        let audioSet = ImmutableHashSet.CreateRange(StringComparer.OrdinalIgnoreCase, audioFilePaths)
        { ResourceId = resourceIdTrimmed
          AudioFilePaths = audioSet
          JsonFilePath = jsonTrimmed
          ImageFilePath = imageTrimmed }

    /// Create a collection of TaggingSets from a collection of file paths related to several video IDs.
    /// Files that don't match the requirements will be ignored.
    static member CreateSets (filePaths: ICollection<string>) : ImmutableList<TaggingSet> =
        if isNull filePaths || filePaths.Count = 0 then
            ImmutableList<TaggingSet>.Empty
        else
            let jsonFileExt = ".json"
            let imageFileExt = ".jpg"

            // Regex: group 1 holds the video id; group 0 is the full filename
            let fileNamesWithVideoIdsRegex =
                Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)", RegexOptions.Compiled)

            filePaths
            |> Seq.map (fun f -> fileNamesWithVideoIdsRegex.Match(f))
            |> Seq.filter (fun m -> m.Success)
            |> Seq.map (fun m -> m.Groups.[0].Value, m.Groups.[1].Value) // (fullFilename, videoId)
            |> Seq.groupBy snd // group by videoId -> seq of (fullFilename, videoId)
            |> Seq.map (fun (videoId, seqFiles) -> videoId, seqFiles |> Seq.map fst)
            |> Seq.filter (fun (_videoId, files) ->
                let filesList = files |> Seq.toList
                // contains at least one audio file
                filesList
                |> Seq.exists (fun f -> PostProcessor.AudioExtensions.CaseInsensitiveContains(Path.GetExtension(f)))
                // exactly one json and exactly one image
                && (filesList |> Seq.filter (fun f -> f.EndsWith(jsonFileExt, StringComparison.OrdinalIgnoreCase)) |> Seq.length = 1)
                && (filesList |> Seq.filter (fun f -> f.EndsWith(imageFileExt, StringComparison.OrdinalIgnoreCase)) |> Seq.length = 1)
            )
            |> Seq.map (fun (videoId, files) ->
                let filesList = files |> Seq.toList
                let audioFiles =
                    filesList
                    |> Seq.filter (fun f -> PostProcessor.AudioExtensions.CaseInsensitiveContains(Path.GetExtension(f)))
                    |> Seq.toList
                    :> ICollection<string>
                let jsonFile = filesList |> Seq.find (fun f -> f.EndsWith(jsonFileExt, StringComparison.OrdinalIgnoreCase))
                let imageFile = filesList |> Seq.find (fun f -> f.EndsWith(imageFileExt, StringComparison.OrdinalIgnoreCase))
                TaggingSet.CreateValidated(videoId, audioFiles, jsonFile, imageFile)
            )
            |> ImmutableList.CreateRange
