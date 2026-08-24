module RenamerTests

open CCVTAC.Main
open CCVTAC.Main.Settings.Settings
open CCVTAC.Main.PostProcessing
open CCFSharpUtils
open System
open Xunit

module UpdateTextViaPatternsTests =

    [<Fact>]
    let ``Renames files per specified rename patterns`` () =
        let patterns : RenamePattern list =
            [
                { RegexPattern = "\s\[[\w_-]{11}\](?=\.\w{3,5})"
                  ReplaceWithPattern = String.Empty
                  Summary = "Remove trailing video IDs (run first)" }
                { RegexPattern = "\s{2,}"
                  ReplaceWithPattern = " "
                  Summary = "Remove multiple spaces" }
                { RegexPattern = " \(字幕\)"
                  ReplaceWithPattern = String.Empty
                  Summary = "Remove 字幕 label" }
                { RegexPattern = "^(.+?)「(.+?)」（([12]\d{3})）"
                  ReplaceWithPattern = "%<1>s - %<2>s [%<3>s]"
                  Summary = "ARTIST「TITLE」（YEAR）" }
                { RegexPattern = "\s+(?=\.\w{3,5})"
                  ReplaceWithPattern = String.Empty
                  Summary = "Remove trailing spaces before the file extension" }
            ]

        let fileName = SB "ARTIST「TITLE」（1923）　　　 (字幕)  [5B1rB894B1U].m4a"

        let expected = "ARTIST - TITLE [1923].m4a"

        let actual =
            List.fold
                (fun sb pattern -> Renamer.updateTextViaPattern (Printer false) true sb pattern)
                fileName
                patterns
            |> _.ToString()

        Assert.Equal(expected, actual)
