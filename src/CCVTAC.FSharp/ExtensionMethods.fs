namespace CCVTAC.Console

open System
open System.IO
open System.Text
open System.Collections.Generic
open System.Linq

module ExtensionMethods =

    /// Determines whether a string contains any text.
    /// allowWhiteSpace = true allows whitespace to count as text.
    let HasText (maybeText: string) (allowWhiteSpace: bool) =
        if allowWhiteSpace then
            not (String.IsNullOrEmpty maybeText)
        else
            not (String.IsNullOrWhiteSpace maybeText)

    /// Overload with default parameter for F# callers.
    let HasTextDefault (maybeText: string) = HasText maybeText false

    /// Collection helpers (similar to the original extension members).
    module SeqEx =
        /// Determines whether a sequence is empty.
        let None (collection: seq<'T>) : bool =
            Seq.isEmpty collection

        /// Determines whether no elements of a sequence satisfy a given condition.
        let NoneBy (predicate: 'T -> bool) (collection: seq<'T>) : bool =
            not (Seq.exists predicate collection)

    /// Case-insensitive contains for a sequence of strings.
    let CaseInsensitiveContains (collection: seq<string>) (text: string) : bool =
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
                invalidArg "replaceWith" (sprintf "The replacement char ('%c') must be a valid path character." replaceWith)

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
