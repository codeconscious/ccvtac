namespace CCVTAC.Console

open System
open System.IO
open System.Text

[<AutoOpen>]
module Utilities =

    let newLine = Environment.NewLine

    /// Determines whether a string contains any text.
    /// allowWhiteSpace = true allows whitespace to count as text.
    let hasText text whiteSpaceCounts =
        let f = if whiteSpaceCounts then String.IsNullOrEmpty else String.IsNullOrWhiteSpace
        not (f text)

    let hasNonWhitespaceText text = hasText text false

    let caseInsensitiveContains (xs: string seq) text : bool =
        xs |> Seq.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))

    /// Returns a new string in which all invalid path characters for the current OS
    /// have been replaced by the specified replacement character.
    /// Throws if the replacement character is an invalid path character.
    let replaceInvalidPathChars
        (replaceWith: char option)
        (customInvalidChars: char list option)
        (text: string)
        : string =

        let replaceWith = defaultArg replaceWith '_'
        let custom = defaultArg customInvalidChars []

        let invalidChars =
            seq {
                yield! Path.GetInvalidFileNameChars()
                yield! Path.GetInvalidPathChars()
                yield Path.PathSeparator
                yield Path.DirectorySeparatorChar
                yield Path.AltDirectorySeparatorChar
                yield Path.VolumeSeparatorChar
                yield! custom
            }
            |> Set.ofSeq

        if invalidChars.Contains replaceWith then
            invalidArg "replaceWith" $"The replacement char ('%c{replaceWith}') must be a valid path character."

        Set.fold
            (fun (sb: StringBuilder) ch -> sb.Replace(ch, replaceWith))
            (StringBuilder text)
            invalidChars
        |> _.ToString()

    let trimTerminalLineBreak (text: string) : string =
        if hasNonWhitespaceText text then
            text.TrimEnd(newLine.ToCharArray())
        else
            text
