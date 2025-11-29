namespace CCVTAC.Console

open System
open System.IO
open System.Text

type SB = StringBuilder

[<AutoOpen>]
module String =

    let newLine = Environment.NewLine

    let hasNoText text =
        String.IsNullOrWhiteSpace text

    let hasText text =
        not (hasNoText text)

    let allHaveText xs =
        xs |> List.forall hasText

    let equalIgnoringCase x y =
        String.Equals(x, y, StringComparison.OrdinalIgnoreCase)

    let endsWithIgnoringCase endingText (text: string) =
        text.EndsWith(endingText, StringComparison.InvariantCultureIgnoreCase)

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

        if invalidChars |> Set.contains replaceWith  then
            invalidArg "replaceWith" $"The replacement char ('%c{replaceWith}') must be a valid path character."

        Set.fold
            (fun (sb: SB) ch -> sb.Replace(ch, replaceWith))
            (SB text)
            invalidChars
        |> _.ToString()

    let trimTerminalLineBreak (text: string) =
        text.TrimEnd(newLine.ToCharArray())

[<AutoOpen>]
module Seq =
    let caseInsensitiveContains text (xs: string seq) : bool =
        xs |> Seq.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))

[<AutoOpen>]
module List =
    let isNotEmpty l = not (List.isEmpty l)

[<AutoOpen>]
module Array =
    let doesNotContain x arr = Array.contains x arr |> not
