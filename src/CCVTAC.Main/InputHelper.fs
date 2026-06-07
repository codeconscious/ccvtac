namespace CCVTAC.Main

open System.Text.RegularExpressions

module InputHelper =

    let prompt = $"Enter one or more YouTube media URLs or commands (or \"%s{Commands.helpCommand}\"):\n▶︎"

    /// A regular expression that detects the beginnings of URLs and application commands within input strings.
    let private userInputRegex = Regex(@"(?:https:|\\)", RegexOptions.Compiled)

    type private IndexPair = { Start: int; End: int }

    type InputCategory = Url | Command

    type CategorizedInput = { Text: string; Category: InputCategory }

    type CategoryCounts (counts: Map<InputCategory, int>) =
        member _.Item
            with get (category: InputCategory) =
                match counts.TryGetValue category with
                | true, v -> v
                | _ -> 0

    /// Takes a user input string and splits it into a collection of inputs.
    let splitInputText input : string list =
        let matches = userInputRegex.Matches input |> Seq.cast<Match> |> Seq.toList

        let split (matches: Match list) =
            let startIndices = matches |> List.map _.Index

            let indexPairs : IndexPair list =
                startIndices
                |> List.mapi (fun idx startIndex ->
                    let endIndex =
                        if idx = startIndices.Length - 1
                        then input.Length
                        else startIndices[idx + 1]
                    { Start = startIndex
                      End   = endIndex })

            indexPairs
            |> List.map (fun pair -> input[pair.Start..(pair.End - 1)].Trim())
            |> List.distinct

        match matches with
        | [] -> []
        | [_] -> [input]
        | _ -> split matches

    let categorizeInputs inputs : CategorizedInput list =
        inputs
        |> List.map (fun input ->
            { Text = input
              Category = if input[0] = Commands.prefix then Command else Url })

    let countCategories (inputs: CategorizedInput list) : CategoryCounts =
        inputs
        |> List.groupBy _.Category
        |> List.map (fun (category, items) -> category, Seq.length items)
        |> Map.ofSeq
        |> CategoryCounts
