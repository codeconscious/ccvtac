namespace CCVTAC.Console

open System
open System.Linq
open System.Text.RegularExpressions
open System.Collections.Generic
open System.Collections.Immutable

module InputHelper =

    let internal Prompt =
        sprintf "Enter one or more YouTube media URLs or commands (or \"%s\"):\n▶︎" Commands.HelpCommand

    /// A regular expression that detects where commands and URLs begin in input strings.
    let private userInputRegex = Regex(@"(?:https:|\\)", RegexOptions.Compiled)

    type private IndexPair = { Start: int; End: int }

    /// Takes a user input string and splits it into a collection of inputs
    /// based upon substrings detected by the class's regular expression pattern.
    let SplitInput (input: string) : ImmutableArray<string> =
        let matches = userInputRegex.Matches(input) |> Seq.cast<Match> |> Seq.toArray

        if matches.Length = 0 then
            ImmutableArray.Empty
        elif matches.Length = 1 then
            ImmutableArray.Create input
        else
            let startIndices = matches |> Array.map _.Index

            let indexPairs =
                startIndices
                |> Array.mapi (fun i startIndex ->
                    let endIndex =
                        if i = startIndices.Length - 1 then input.Length else startIndices[i + 1]
                    { Start = startIndex; End = endIndex })

            let splitInputs =
                indexPairs
                |> Array.map (fun p -> input[p.Start..(p.End - 1)].Trim())
                |> Array.distinct

            ImmutableArray.CreateRange splitInputs

    type InputCategory =
        | Url
        | Command

    type CategorizedInput = { Text: string; Category: InputCategory }

    let CategorizeInputs (inputs: ICollection<string>) : CategorizedInput list =
        inputs
        |> Seq.cast<string>
        |> Seq.map (fun input ->
            let category =
                if input.StartsWith(string Commands.Prefix)
                then InputCategory.Command
                else InputCategory.Url
            { Text = input; Category = category })
        |> List.ofSeq

    type CategoryCounts(counts: Map<InputCategory,int>) =
        member _.Item
            with get (category: InputCategory) =
                match counts.TryGetValue category with
                | true, v -> v
                | _ -> 0

    let CountCategories (inputs: CategorizedInput seq) : CategoryCounts =
        let counts =
            inputs
            |> Seq.cast<CategorizedInput>
            |> Seq.groupBy (fun i -> i.Category)
            |> Seq.map (fun (k, grp) -> k, grp |> Seq.length)
            // |> dict
            // :?> IDictionary<InputCategory,int>
            // |> fun d -> Dictionary<InputCategory,int>(d)
            |> Map.ofSeq

        CategoryCounts(counts)
