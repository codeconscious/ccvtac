namespace CCVTAC.Console

open System.Text.RegularExpressions

module InputHelper =

    let prompt = $"Enter one or more YouTube media URLs or commands (or \"%s{Commands.helpCommand}\"):\n▶︎"

    /// A regular expression that detects the beginnings of URLs and application commands in input strings.
    let private userInputRegex = Regex(@"(?:https:|\\)", RegexOptions.Compiled)

    type private IndexPair = { Start: int; End: int }

    type InputCategory = Url | Command

    type CategorizedInput = { Text: string; Category: InputCategory }

    let countCategoryItems (category: InputCategory)  (counts: Map<InputCategory,int>) =
        match counts.TryGetValue category with
        | true, c -> c
        | _       -> 0

    /// Takes a user input string and splits it into a collection of inputs.
    let splitInputText input : string list =
        let matches = userInputRegex.Matches input |> Seq.cast<Match> |> Seq.toList

        match matches with
        | [] -> []
        | [_] -> [input]
        | _ ->
            let startIndices = matches |> List.map _.Index

            let indexPairs : IndexPair list =
                startIndices
                |> List.mapi (fun idx startIndex ->
                    let endIndex =
                        if idx = startIndices.Length - 1
                        then input.Length
                        else startIndices[idx + 1]
                    { Start = startIndex
                      End = endIndex })

            indexPairs
            |> List.map (fun p -> input[p.Start..(p.End - 1)].Trim())
            |> List.distinct

    let categorizeInputs inputs : CategorizedInput list =
        inputs
        |> List.map (fun input ->
            { Text = input
              Category = if input.StartsWith(string Commands.prefix)
                         then InputCategory.Command
                         else InputCategory.Url })

    let countCategories (inputs: CategorizedInput list) : Map<InputCategory, int> =
        inputs
        |> List.groupBy _.Category
        |> List.map (fun (k, grp) -> k, grp |> Seq.length)
        |> Map.ofSeq
