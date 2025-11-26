namespace CCVTAC.Console.PostProcessing

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open CCVTAC.Console
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings
open Startwatch.Library
open ExtensionMethods

module Renamer =

    let private getNormalizationForm (form: string) =
        match form.Trim().ToUpperInvariant() with
        | "D" -> NormalizationForm.FormD
        | "KD" -> NormalizationForm.FormKD
        | "KC" -> NormalizationForm.FormKC
        | _ -> NormalizationForm.FormC

    let Run (settings: UserSettings) (workingDirectory: string) (printer: Printer) : unit =
        let watch = Watch()

        let workingDirInfo = DirectoryInfo(workingDirectory)

        let audioFiles =
            workingDirInfo.EnumerateFiles()
            |> Seq.filter (fun f -> caseInsensitiveContains AudioExtensions f.Extension)
            |> List.ofSeq

        if audioFiles.Length = 0 then
            printer.Warning "No audio files to rename were found."
        else
            printer.Debug $"Renaming %d{audioFiles.Length} audio file(s)..."

            for file in audioFiles do
                let newFileName =
                    // Fold over rename patterns, starting with StringBuilder(file.Name)
                    settings.RenamePatterns
                    |> Seq.fold
                        (fun (sb: StringBuilder) (renamePattern) ->
                            let regex = Regex(renamePattern.RegexPattern)
                            let matches =
                                regex.Matches(sb.ToString())
                                |> Seq.cast<Match>
                                |> Seq.filter (fun m -> m.Success)
                                |> Seq.rev
                                |> Seq.toList

                            if matches.Length = 0 then sb
                            else
                                if not settings.QuietMode then
                                    let matchedPatternSummary =
                                        // if isNull renamePattern.Summary then // TODO: Check on this.
                                            $"`%s{renamePattern.RegexPattern}` (no description)"
                                        // else
                                        //     sprintf "\"%s\"" renamePattern.Summary

                                    printer.Debug(sprintf "Rename pattern %s matched × %d." matchedPatternSummary matches.Length)

                                for m in matches do
                                    // remove matched substring
                                    sb.Remove(m.Index, m.Length) |> ignore

                                    // build replacement text by replacing %<n>s placeholders with group captures
                                    let replacementText =
                                        m.Groups
                                        |> Seq.cast<Group>
                                        |> Seq.mapi (fun i g ->
                                            let searchFor = sprintf "%%<%d>s" (i + 1)
                                            let replaceWith =
                                                // Group 0 is the entire match, and we only want groups starting at 1.
                                                if i + 1 < m.Groups.Count then m.Groups[i + 1].Value.Trim() else String.Empty
                                            (searchFor, replaceWith))
                                        |> Seq.fold (fun (sbRep: StringBuilder) (searchFor, replaceWith) ->
                                            sbRep.Replace(searchFor, replaceWith))
                                            (StringBuilder(renamePattern.ReplaceWithPattern))
                                        |> _.ToString()

                                    sb.Insert(m.Index, replacementText) |> ignore

                                sb)
                        (StringBuilder(file.Name))
                    |> _.ToString()

                try
                    let dest =
                        Path.Combine(workingDirectory, newFileName)
                        |> _.Normalize(getNormalizationForm settings.NormalizationForm)

                    File.Move(file.FullName, dest)
                    printer.Debug (sprintf "• From: \"%s\"" file.Name)
                    printer.Debug (sprintf "    To: \"%s\"" newFileName)
                with ex ->
                    printer.Error (sprintf "• Error renaming \"%s\": %s" file.Name ex.Message)

            printer.Info (sprintf "Renaming done in %s." watch.ElapsedFriendly)
