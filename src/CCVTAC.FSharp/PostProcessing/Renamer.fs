namespace CCVTAC.Console.PostProcessing

open CCVTAC.Console
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.Settings.Settings
open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open Startwatch.Library

module Renamer =

    let private toNormalizationForm (form: string) =
        match form.Trim().ToUpperInvariant() with
        | "D"  -> NormalizationForm.FormD
        | "KD" -> NormalizationForm.FormKD
        | "KC" -> NormalizationForm.FormKC
        | _    -> NormalizationForm.FormC

    let updateTextViaPatterns isQuietMode (printer: Printer) (sb: SB) (renamePattern: RenamePattern) =
        let regex = Regex renamePattern.RegexPattern

        let matches =
            regex.Matches(sb.ToString())
            |> Seq.cast<Match>
            |> Seq.filter _.Success
            |> Seq.rev
            |> Seq.toList

        if List.isEmpty matches
        then sb
        else
            if not isQuietMode then
                let patternSummary =
                    if String.hasText renamePattern.Summary then
                        $"\"%s{renamePattern.Summary}\""
                    else
                        $"`%s{renamePattern.RegexPattern}` (no description)"

                printer.Debug $"Rename pattern %s{patternSummary} matched × %d{matches.Length}."

            for m in matches do
                sb.Remove(m.Index, m.Length) |> ignore

                // Build replacement text by replacing %<n> placeholders with group captures.
                let replacementText =
                    m.Groups
                    |> Seq.cast<Group>
                    |> Seq.mapi (fun i _ ->
                        let searchFor = sprintf "%%<%d>s" (i + 1)
                        let replaceWith =
                            // Group 0 is the entire match, so we only want groups starting at 1.
                            if i + 1 < m.Groups.Count
                            then m.Groups[i + 1].Value.Trim()
                            else String.Empty
                        (searchFor, replaceWith))
                    |> Seq.fold (fun (sb': SB) -> sb'.Replace) (SB renamePattern.ReplaceWithPattern)
                    |> _.ToString()

                sb.Insert(m.Index, replacementText) |> ignore

            sb

    let run userSettings workingDirectory (printer: Printer) : unit =
        let watch = Watch()
        let workingDirInfo = DirectoryInfo workingDirectory

        let audioFiles =
            workingDirInfo.EnumerateFiles()
            |> Seq.filter (fun f -> List.caseInsensitiveContains f.Extension Files.audioFileExtensions)
            |> List.ofSeq

        if List.isEmpty audioFiles then
            printer.Warning "No audio files to rename were found."
        else
            printer.Debug $"""Renaming %s{String.fileLabel (Some "audio") audioFiles.Length}..."""

            for audioFile in audioFiles do
                let newFileName =
                    userSettings.RenamePatterns
                    |> List.fold
                        (fun (sb: SB) -> updateTextViaPatterns userSettings.QuietMode printer sb)
                        (SB audioFile.Name)
                    |> _.ToString()

                try
                    let destinationPath =
                        Path.Combine(workingDirectory, newFileName)
                        |> _.Normalize(toNormalizationForm userSettings.NormalizationForm)

                    File.Move(audioFile.FullName, destinationPath)

                    printer.Debug $"• From: \"%s{audioFile.Name}\""
                    printer.Debug $"    To: \"%s{newFileName}\""
                with ex ->
                    printer.Error $"• Error renaming \"%s{audioFile.Name}\": %s{ex.Message}"

            printer.Info $"Renaming done in %s{watch.ElapsedFriendly}."
