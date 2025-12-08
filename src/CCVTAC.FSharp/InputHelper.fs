namespace CCVTAC.Console

open System.Text.RegularExpressions

module InputHelper =

    let prompt = $"Enter one or more YouTube media URLs or commands (or \"%s{Commands.helpCommand}\"):\n▶︎"

    /// A regular expression that detects the beginnings of URLs and application commands in input strings.
    let private userInputRegex = Regex(@"(?:https:|\\)", RegexOptions.Compiled)

    type private IndexPair = { Start: int; End: int }

    type InputCategory = Url | Command

    type CategorizedInput = { Text: string; Category: InputCategory }

    type CategoryCounts (counts: Map<InputCategory,int>) =
        member _.Item
            with get (category: InputCategory) =
                match counts.TryGetValue category with
                | true, v -> v
                | _ -> 0

    /// Takes a user input string and splits it into a collection of inputs.
    let splitInputText input : string list =
        let matches = userInputRegex.Matches input |> Seq.cast<Match> |> Seq.toList

        match matches with
        | [] -> []
        | [_] -> [input]
        | _ ->
            let startIndices = matches |> List.map _.Index

            let indexPairs =
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
        |> Seq.cast<string>
        |> Seq.map (fun input ->
            { Text = input
              Category = if input.StartsWith(string Commands.prefix)
                         then InputCategory.Command
                         else InputCategory.Url })
        |> List.ofSeq

    let countCategories (inputs: CategorizedInput seq) : CategoryCounts =
        let counts =
            inputs
            |> Seq.cast<CategorizedInput>
            |> Seq.groupBy _.Category
            |> Seq.map (fun (k, grp) -> k, grp |> Seq.length)
            |> Map.ofSeq

        CategoryCounts counts
