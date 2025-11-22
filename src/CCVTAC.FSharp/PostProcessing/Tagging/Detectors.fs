namespace CCVTAC.Console.PostProcessing.Tagging

open System
open System.Text.RegularExpressions
open CCVTAC.FSharp.Settings

module Detectors =
    /// Finds and returns the first instance of text matching a given detection scheme pattern,
    /// parsing into T if necessary.
    /// <returns>A match of type T if there was a match; otherwise, the default value provided.</returns>
    let internal detectSingle<'T>
        (videoMetadata: VideoMetadata)
        (patterns: TagDetectionPattern seq)
        (defaultValue: 'T option) =

        patterns
        |> Seq.tryPick (fun pattern ->
            let fieldText = extractMetadataText videoMetadata pattern.SearchField

            let match' = Regex(pattern.RegexPattern).Match(fieldText)

            if not match'.Success then
                None
            else
                let matchedText =
                    match'.Groups.[pattern.MatchGroup].Value.Trim()

                cast<'T> matchedText defaultValue
        )
        |> Option.defaultValue defaultValue

    /// Finds and returns all instances of text matching a given detection scheme pattern,
    /// concatenating them into a single string (using a custom separator), then casting
    /// to type T if necessary.
    /// <returns>A match of type T if there were any matches; otherwise, the default value provided.</returns>
    let internal detectMultiple<'T>
        (data: VideoMetadata)
        (patterns: TagDetectionPattern seq)
        (defaultValue: 'T option)
        (separator: string) =

        let matchedValues =
            patterns
            |> Seq.collect (fun pattern ->
                let fieldText = extractMetadataText data pattern.SearchField

                Regex(pattern.RegexPattern).Matches(fieldText)
                |> Seq.filter (fun m -> m.Success)
                |> Seq.map (fun m -> m.Groups.[pattern.MatchGroup].Value.Trim())
            )
            |> Seq.distinct
            |> Seq.toArray

        if matchedValues.Length = 0 then
            defaultValue
        else
            let joinedMatchedText = String.Join(separator, matchedValues)
            cast<'T> joinedMatchedText defaultValue

    /// Attempts casting the input text to type T and returning it.
    /// If casting fails, the default value is returned instead.
    let private cast<'T> (text: string option) (defaultValue: 'T option) =
        match text with
        | None -> defaultValue
        | Some textValue ->
            try
                // If T is string, return the text directly
                if typeof<'T> = typeof<string> then
                    Some(box textValue :?> 'T)
                else
                    // Try to convert to the target type
                    Some(Convert.ChangeType(textValue, typeof<'T>) :?> 'T)
            with
            | _ -> defaultValue

    /// Extracts the value of the specified tag field from the given data.
    /// <param name="metadata">Video metadata</param>
    /// <param name="fieldName">The name of the field within the video metadata to read</param>
    /// <returns>The text content of the requested field of the video metadata</returns>
    let private extractMetadataText (metadata: VideoMetadata) (fieldName: string) =
        match fieldName with
        | "title" -> metadata.Title
        | "description" -> metadata.Description
        | _ ->
            // TODO: It would be best to check for invalid entries upon settings deserialization.
            raise (ArgumentException($"\"{fieldName}\" is an invalid video metadata field name."))
