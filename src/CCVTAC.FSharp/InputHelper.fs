namespace CCVTAC.Console

open System.Text.RegularExpressions
open System.Collections.Generic

module InputHelper =

    let prompt =
        $"Enter one or more YouTube media URLs or commands (or \"%s{Commands.helpCommand}\"):\n▶︎"

    /// A regular expression that detects where commands and URLs begin in input strings.
    let private userInputRegex = Regex(@"(?:https:|\\)", RegexOptions.Compiled)

    type private IndexPair = { Start: int; End: int }

    type InputCategory =
        | Url
        | Command

    type CategorizedInput = { Text: string; Category: InputCategory }

    type CategoryCounts(counts: Map<InputCategory,int>) =
        member _.Item
            with get (category: InputCategory) =
                match counts.TryGetValue category with
                | true, v -> v
                | _ -> 0

    /// Takes a user input string and splits it into a collection of inputs
    /// based upon substrings detected by the class's regular expression pattern.
    let splitInput (input: string) : string array =
        let matches = userInputRegex.Matches(input) |> Seq.cast<Match> |> Seq.toArray

        if isZero matches.Length then
            [| |]
        elif isOne matches.Length then
            [| input |]
        else
            let startIndices = matches |> Array.map _.Index

            let indexPairs =
                startIndices
                |> Array.mapi (fun idx startIndex ->
                    let endIndex =
                        if idx = startIndices.Length - 1
                        then input.Length
                        else startIndices[idx + 1]
                    { Start = startIndex; End = endIndex })

            indexPairs
            |> Array.map (fun p -> input[p.Start..(p.End - 1)].Trim())
            |> Array.distinct

    let categorizeInputs (inputs: ICollection<string>) : CategorizedInput list =
        inputs
        |> Seq.cast<string>
        |> Seq.map (fun input ->
            let category =
                if input.StartsWith(string Commands.prefix)
                then InputCategory.Command
                else InputCategory.Url
            { Text = input; Category = category })
        |> List.ofSeq

    let countCategories (inputs: CategorizedInput seq) : CategoryCounts =
        let counts =
            inputs
            |> Seq.cast<CategorizedInput>
            |> Seq.groupBy (fun i -> i.Category)
            |> Seq.map (fun (k, grp) -> k, grp |> Seq.length)
            |> Map.ofSeq

        CategoryCounts(counts)
