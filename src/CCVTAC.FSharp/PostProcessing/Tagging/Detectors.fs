namespace CCVTAC.Console.PostProcessing.Tagging

open CCVTAC.Console
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console.PostProcessing
open System
open System.Text.RegularExpressions

module Detectors =
    /// Attempts casting the input text to type 'a and returning it.
    /// If casting fails, the default value is returned instead.
    let tryCast<'a> (text: string) : 'a option =
        try
            // If T is string, return the text directly
            if typeof<'a> = typeof<string> then
                Some (box text :?> 'a)
            else
                Some (Convert.ChangeType(text, typeof<'a>) :?> 'a)
        with
        | _ -> None

    /// Extracts the value of the specified tag field from the given data.
    /// <param name="metadata">Video metadata</param>
    /// <param name="fieldName">The name of the field within the video metadata to read</param>
    /// <returns>The text content of the requested field of the video metadata</returns>
    let private extractMetadataText (metadata: VideoMetadata) (fieldName: string) : string =
        match fieldName with
        | "title" -> metadata.Title
        | "description" -> metadata.Description
        | _ ->
            // TODO: It would be best to check for invalid entries upon settings deserialization.
            raise (ArgumentException($"\"{fieldName}\" is an invalid video metadata field name."))

    /// Finds and returns the first instance of text matching a given detection scheme pattern,
    /// parsing into T if necessary.
    /// <returns>A match of type 'a if there was a match; otherwise, the default value provided.</returns>
    let detectSingle<'a>
        (videoMetadata: VideoMetadata)
        (patterns: TagDetectionPattern seq)
        (defaultValue: 'a option)
        : 'a option =

        patterns
        |> Seq.tryPick (fun pattern ->
            let fieldText = extractMetadataText videoMetadata pattern.SearchField
            let match' = Regex(pattern.RegexPattern).Match(fieldText)

            if match'.Success then
                let matchedText = match'.Groups[pattern.MatchGroup].Value.Trim()
                tryCast<'a> matchedText
            else
                None)
        |> Option.orElse defaultValue

    /// Finds and returns all instances of text matching a given detection scheme pattern,
    /// concatenating them into a single string (using a custom separator), then casting
    /// to type 'a if necessary.
    /// <returns>A match of type 'a if there were any matches; otherwise, the default value provided.</returns>
    let detectMultiple<'a>
        (data: VideoMetadata)
        (patterns: TagDetectionPattern seq)
        (defaultValue: 'a option)
        (separator: string)
        : 'a option =

        let matchedValues =
            patterns
            |> Seq.collect (fun pattern ->
                let fieldText = extractMetadataText data pattern.SearchField
                Regex(pattern.RegexPattern).Matches(fieldText)
                |> Seq.filter _.Success
                |> Seq.map _.Groups[pattern.MatchGroup].Value.Trim())
            |> Seq.distinct
            |> Seq.toArray

        if Array.isEmpty matchedValues then
            defaultValue
        else
            String.Join(separator, matchedValues)
            |> tryCast<'a>
            |> Option.orElse defaultValue

