namespace CCVTAC.Console

open System
open System.Globalization
open System.IO
open System.Text

type SB = StringBuilder

module Numerics =

    let inline isZero (n: ^a) =
        n = LanguagePrimitives.GenericZero<'a>

    let inline isOne (n: ^a) =
        n = LanguagePrimitives.GenericOne<'a>

    /// Formats a number of any type to a comma-formatted string.
    let inline formatNumber (i: ^T) : string
        when ^T : (member ToString : string * IFormatProvider -> string) =
        (^T : (member ToString : string * IFormatProvider -> string) (i, "#,##0", CultureInfo.InvariantCulture))

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

    let endsWithIgnoreCase endText (text: string) =
        text.EndsWith(endText, StringComparison.InvariantCultureIgnoreCase)

    /// Pluralize text using a specified count.
    let inline pluralize ifOne ifNotOne count =
        if Numerics.isOne count then ifOne else ifNotOne

    /// Pluralize text including its count, such as "1 file", "30 URLs".
    let inline pluralizeWithCount ifOne ifNotOne count =
        sprintf "%d %s" count (pluralize ifOne ifNotOne count)

    let inline private fileLabeller descriptor (count: int) =
        match descriptor with
        | None   -> $"""%s{Numerics.formatNumber count} %s{pluralize "file" "files" count}"""
        | Some d -> $"""%s{Numerics.formatNumber count} %s{d} {pluralize "file" "files" count}"""

    /// Returns a file-count string, such as "0 files" or 1 file" or "140 files".
    let fileLabel count =
        fileLabeller None count

    /// Returns a file-count string with a descriptor, such as "0 audio files" or "140 deleted files".
    let fileLabelWithDescriptor (descriptor: string) count =
        fileLabeller (Some (descriptor.Trim())) count

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
                yield  Path.PathSeparator
                yield  Path.DirectorySeparatorChar
                yield  Path.AltDirectorySeparatorChar
                yield  Path.VolumeSeparatorChar
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

[<RequireQualifiedAccess>]
module Seq =

    let isNotEmpty seq = not (Seq.isEmpty seq)

    let doesNotContain x seq = not <| Seq.contains x seq

    let hasMultiple seq = seq |> Seq.length |> (<) 1

    let caseInsensitiveContains text (xs: string seq) : bool =
        xs |> Seq.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))

[<RequireQualifiedAccess>]
module List =

    let isNotEmpty lst = not (List.isEmpty lst)

    let doesNotContain x lst = not <| List.contains x lst

    let hasMultiple lst = lst |> List.length |> (<) 1

    let caseInsensitiveContains text (lst: string list) : bool =
        lst |> List.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))

[<RequireQualifiedAccess>]
module Array =

    let isNotEmpty arr = not <| Array.isEmpty arr

    let doesNotContain x arr = not <| Array.contains x arr

    let hasMultiple arr = arr |> Array.length |> (<) 1

    let caseInsensitiveContains text (arr: string array) : bool =
        arr |> Array.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))
