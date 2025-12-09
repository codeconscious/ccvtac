namespace CCVTAC.Console

open System
open System.IO
open System.Text

type SB = StringBuilder

module Numerics =

    let inline isZero (n: ^a) =
        n = LanguagePrimitives.GenericZero<'a>

    let inline isOne (n: ^a) =
        n = LanguagePrimitives.GenericOne<'a>

module String =

    let newLine = Environment.NewLine

    let hasNoText text =
        String.IsNullOrWhiteSpace text

    let hasText text =
        not (hasNoText text)

    let allHaveText xs =
        xs |> List.forall hasText

    let textOrFallback fallback text =
        if hasText text then text else fallback

    let textOrEmpty text =
        textOrFallback text String.Empty

    let equalIgnoringCase x y =
        String.Equals(x, y, StringComparison.OrdinalIgnoreCase)

    let startsWithIgnoreCase startText (text: string)  =
        text.StartsWith(startText, StringComparison.InvariantCultureIgnoreCase)

    let endsWithIgnoringCase endText (text: string) =
        text.EndsWith(endText, StringComparison.InvariantCultureIgnoreCase)

    let inline pluralize ifOne ifNotOne count =
        if Numerics.isOne count then ifOne else ifNotOne

    let inline fileLabel descriptor count =
        match descriptor with
        | None   -> $"""%d{count} %s{pluralize "file" "files" count}"""
        | Some d -> $"""%d{count} %s{d} {pluralize "file" "files" count}"""

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

module Seq =

    let isNotEmpty l = not (Seq.isEmpty l)

    let doesNotContain x seq = not <| Seq.contains x seq

    let hasMultiple seq = seq |> Seq.length |> (<) 1

    let caseInsensitiveContains text (xs: string seq) : bool =
        xs |> Seq.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))

module List =

    let isNotEmpty l = not (List.isEmpty l)

    let caseInsensitiveContains text (xs: string list) : bool =
        xs |> List.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))

module Array =

    let isNotEmpty arr = not <| Array.isEmpty arr

    let doesNotContain x arr = not <| Array.contains x arr

    let hasMultiple arr = arr |> Array.length |> (<) 1

    let caseInsensitiveContains text (xs: string array) : bool =
        xs |> Array.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))
