namespace CCVTAC.Console

open System
open System.IO
open System.Text
open System.Collections.Generic

module ExtensionMethods =

    /// Determines whether a string contains any text.
    /// allowWhiteSpace = true allows whitespace to count as text.
    let hasText (maybeText: string) (allowWhiteSpace: bool) =
        if allowWhiteSpace then
            not (String.IsNullOrEmpty maybeText)
        else
            not (String.IsNullOrWhiteSpace maybeText)

    /// Overload with default parameter for F# callers.
    let HasTextDefault (maybeText: string) = hasText maybeText false

    /// Collection helpers (similar to the original extension members).
    module SeqEx =
        /// Determines whether a sequence is empty.
        let None (collection: seq<'a>) : bool =
            Seq.isEmpty collection

        /// Determines whether no elements of a sequence satisfy a given condition.
        let NoneBy (predicate: 'a -> bool) (collection: seq<'a>) : bool =
            not (Seq.exists predicate collection)

    /// Case-insensitive contains for a sequence of strings.
    let caseInsensitiveContains (collection: seq<string>) (text: string) : bool =
        collection
        |> Seq.exists (fun s -> String.Equals(s, text, StringComparison.OrdinalIgnoreCase))

    /// String instance helpers as an F# type extension for System.String.
    type System.String with

        /// Returns a new string in which all invalid path characters for the current OS
        /// have been replaced by the specified replacement character.
        /// Throws if the replacement character is an invalid path character.
        member this.ReplaceInvalidPathChars(?replaceWith: char, ?customInvalidChars: char[]) : string =
            let replaceWith = defaultArg replaceWith '_'
            let custom = defaultArg customInvalidChars [||]

            // Collect invalid characters
            let invalidCharsSeq =
                seq {
                    yield! Path.GetInvalidFileNameChars()
                    yield! Path.GetInvalidPathChars()
                    yield Path.PathSeparator
                    yield Path.DirectorySeparatorChar
                    yield Path.AltDirectorySeparatorChar
                    yield Path.VolumeSeparatorChar
                    yield! custom
                }
                |> Seq.distinct

            let invalidSet = HashSet<char>(invalidCharsSeq)

            if invalidSet.Contains(replaceWith) then
                invalidArg "replaceWith" $"The replacement char ('%c{replaceWith}') must be a valid path character."

            // Replace each invalid char in the string using StringBuilder for efficiency
            let sb = StringBuilder(this)
            for ch in invalidSet do
                sb.Replace(ch, replaceWith) |> ignore
            sb.ToString()

        /// Trims trailing newline characters (Environment.NewLine) from the end of the string.
        member this.TrimTerminalLineBreak() : string =
            if HasTextDefault this then
                this.TrimEnd(Environment.NewLine.ToCharArray())
            else
                this


module Utilities =
    /// Returns a new string in which all invalid path characters for the current OS
    /// have been replaced by the specified replacement character.
    /// Throws if the replacement character is an invalid path character.
    let replaceInvalidPathChars (replaceWith: char option) (customInvalidChars: char array option) (text: string) : string =
        let replaceWith = defaultArg replaceWith '_'
        let custom = defaultArg customInvalidChars [||]

        let invalidCharsSeq =
            seq {
                yield! Path.GetInvalidFileNameChars()
                yield! Path.GetInvalidPathChars()
                yield Path.PathSeparator
                yield Path.DirectorySeparatorChar
                yield Path.AltDirectorySeparatorChar
                yield Path.VolumeSeparatorChar
                yield! custom
            }
            |> Seq.distinct

        let invalidSet = HashSet<char>(invalidCharsSeq)

        if invalidSet.Contains(replaceWith)
        then invalidArg "replaceWith" $"The replacement char ('%c{replaceWith}') must be a valid path character."

        // Replace each invalid char in the string. // TODO: `fold`/`reduce` this up and return a Result!
        let sb = StringBuilder text
        for ch in invalidSet do
            sb.Replace(ch, replaceWith) |> ignore
        sb.ToString()

