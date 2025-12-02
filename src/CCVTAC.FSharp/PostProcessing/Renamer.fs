namespace CCVTAC.Console.PostProcessing

open CCVTAC.Console
open CCVTAC.Console.Settings.Settings
open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open Startwatch.Library

module Renamer =

    let private toNormalizationForm (form: string) =
        match form.Trim().ToUpperInvariant() with
        | "D" -> NormalizationForm.FormD
        | "KD" -> NormalizationForm.FormKD
        | "KC" -> NormalizationForm.FormKC
        | _ -> NormalizationForm.FormC

    let Run userSettings workingDirectory (printer: Printer) : unit =
        let watch = Watch()
        let workingDirInfo = DirectoryInfo workingDirectory

        let audioFiles =
            workingDirInfo.EnumerateFiles()
            |> Seq.filter (fun f -> Seq.caseInsensitiveContains f.Extension AudioExtensions)
            |> List.ofSeq

        if List.isEmpty audioFiles then
            printer.Warning "No audio files to rename were found."
        else
            printer.Debug $"Renaming %d{audioFiles.Length} audio file(s)..."

            for file in audioFiles do
                let newFileName =
                    userSettings.RenamePatterns
                    |> Array.fold
                        (fun (sb: StringBuilder) renamePattern ->
                            let regex = Regex renamePattern.RegexPattern
                            let matches =
                                regex.Matches(sb.ToString())
                                |> Seq.cast<Match>
                                |> Seq.filter _.Success
                                |> Seq.rev
                                |> Seq.toList

                            if matches.Length = 0 then sb
                            else
                                if not userSettings.QuietMode then
                                    let patternSummary =
                                        // if isNull renamePattern.Summary then // TODO: Check on this.
                                            $"`%s{renamePattern.RegexPattern}` (no description)"
                                        // else
                                        //     sprintf "\"%s\"" renamePattern.Summary

                                    printer.Debug $"Rename pattern %s{patternSummary} matched × %d{matches.Length}."

                                for m in matches do
                                    sb.Remove(m.Index, m.Length) |> ignore

                                    // Build replacement text by replacing %<n>s placeholders with group captures
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
                                        |> Seq.fold (fun (sbRep: StringBuilder) (searchFor, replaceWith) ->
                                            sbRep.Replace(searchFor, replaceWith))
                                            (StringBuilder(renamePattern.ReplaceWithPattern))
                                        |> _.ToString()

                                    sb.Insert(m.Index, replacementText) |> ignore

                                sb)
                        (StringBuilder file.Name)
                    |> _.ToString()

                try
                    let dest =
                        Path.Combine(workingDirectory, newFileName)
                        |> _.Normalize(toNormalizationForm userSettings.NormalizationForm)

                    File.Move(file.FullName, dest)
                    printer.Debug $"• From: \"%s{file.Name}\""
                    printer.Debug $"    To: \"%s{newFileName}\""
                with ex ->
                    printer.Error $"• Error renaming \"%s{file.Name}\": %s{ex.Message}"

            printer.Info $"Renaming done in %s{watch.ElapsedFriendly}."
