namespace CCVTAC.Main.PostProcessing

open CCVTAC.Main
open CCVTAC.Main.IoUtilities
open CCVTAC.Main.Settings.Settings
open CCFSharpUtils
open CCFSharpUtils.Collections
open CCFSharpUtils.Text
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

    let updateTextViaPattern isQuietMode (printer: Printer) (text: SB) (renamePattern: RenamePattern) : SB =
        let regex = Regex renamePattern.RegexPattern

        let matches = text.ToString() |> regex.Matches |> Rgx.successMatches |> Seq.rev |> Seq.toList

        let printSummary: unit =
            let patternDesc = if String.hasText renamePattern.Summary
                              then $"\"%s{renamePattern.Summary}\""
                              else $"`%s{renamePattern.RegexPattern}` (no description)"
            printer.Debug $"> Rename pattern %s{patternDesc} matched (%d{matches.Length}×)."

        /// Builds replacement text by substituting %<n>s placeholders with captured regex group values.
        /// Group values are trimmed, and indexing starts from 1 (because group 0 is the full match).
        let buildReplacementText (renamePattern: RenamePattern) (m: Match) : string =
            let sb = SB renamePattern.ReplaceWithPattern

            for i = 1 to m.Groups.Count - 1 do
                let placeholder = sprintf "%%<%d>s" i
                let value = if i < m.Groups.Count then m.Groups[i].Value.Trim() else String.Empty
                sb.Replace(placeholder, value) |> ignore

            sb.ToString()

        if List.isEmpty matches then
            text
        else
            if not isQuietMode then printSummary

            for m in matches do
                let replacementText = buildReplacementText renamePattern m
                text.Remove(m.Index, m.Length).Insert(m.Index, replacementText) |> ignore

            text

    let run userSettings workingDirectory (printer: Printer) : unit =
        let watch = Watch()
        let workingDirInfo = DirectoryInfo workingDirectory

        let audioFiles =
            workingDirInfo.EnumerateFiles()
            |> Seq.filter (fun f -> List.containsIgnoreCase f.Extension Files.audioFileExts)
            |> List.ofSeq

        match audioFiles with
        | [] ->
            printer.Warning "No audio files to rename were found."
        | _ ->
            printer.Debug $"""Renaming %s{String.fileLabelWithDesc "audio" audioFiles.Length}..."""

            for audioFile in audioFiles do
                let newFileName =
                    userSettings.RenamePatterns
                    |> List.fold
                        (fun (sb: SB) -> updateTextViaPattern userSettings.QuietMode printer sb)
                        (SB audioFile.Name)
                    |> _.ToString()

                try
                    let destinationPath =
                        Path.Combine(workingDirectory, newFileName)
                            .Normalize(toNormalizationForm userSettings.NormalizationForm)

                    File.Move(audioFile.FullName, destinationPath)

                    printer.Debug $"• From: \"%s{audioFile.Name}\""
                    printer.Debug $"    To: \"%s{newFileName}\""
                with exn ->
                    printer.Error $"• Error renaming \"%s{audioFile.Name}\": %s{exn.Message}"

            printer.Info $"Renaming done in %s{watch.ElapsedFriendly}."
