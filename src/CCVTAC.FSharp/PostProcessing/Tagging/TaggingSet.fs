namespace CCVTAC.Console.PostProcessing.Tagging

open CCVTAC.Console
open System
open System.IO
open System.Text.RegularExpressions
open System.Collections.Generic
open System.Collections.Immutable

/// Contains all the data necessary for tagging a related set of files.
[<Struct>]
type TaggingSet =
    { ResourceId: string
      AudioFilePaths: string list
      JsonFilePath: string
      ImageFilePath: string }

    member this.AllFiles : string list =
        List.concat [this.AudioFilePaths; [this.JsonFilePath; this.ImageFilePath]]

    /// Create a collection of TaggingSets from a collection of file paths related to several video IDs.
    /// Files that don't match the requirements will be ignored.
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
            |> Seq.filter (fun (_, fileNames) ->
                let isSupportedExtension =
                    fileNames
                    |> Seq.exists (fun f ->
                        let f' = match Path.GetExtension (f: string) with Null -> "" | NonNull (x: string) -> x // TODO: Improve.
                        caseInsensitiveContains f' AudioExtensions)
                let jsonCount = fileNames |> Seq.filter (endsWithIgnoringCase jsonFileExt) |> Seq.length
                let imageCount = fileNames |> Seq.filter (endsWithIgnoringCase imageFileExt) |> Seq.length
                isSupportedExtension && jsonCount = 1 && imageCount = 1)
            |> Seq.map (fun (key, files) ->
                let filesArr = files |> Seq.toArray
                let audioFiles =
                    filesArr
                    |> Seq.filter (fun f ->
                        let f' = match Path.GetExtension (f: string) with Null -> "" | NonNull (x: string) -> x // TODO: Improve.
                        caseInsensitiveContains f' AudioExtensions)
                    |> Seq.toList
                let jsonFile = filesArr |> Seq.find (endsWithIgnoringCase jsonFileExt)
                let imageFile = filesArr |> Seq.find (endsWithIgnoringCase imageFileExt)

                { ResourceId = key
                  AudioFilePaths = audioFiles
                  JsonFilePath = jsonFile
                  ImageFilePath = imageFile })
            |> List.ofSeq
