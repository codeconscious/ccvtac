namespace CCVTAC.Console.PostProcessing

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open System.Linq
open System.Collections.Immutable
open CCVTAC.FSharp.Settings

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
            |> Seq.filter (fun f -> PostProcessor.AudioExtensions.CaseInsensitiveContains(f.Extension))
            |> Seq.toImmutableList

        if audioFiles.None() then
            printer.Warning "No audio files to rename were found."
        else
            printer.Debug (sprintf "Renaming %d audio file(s)..." audioFiles.Count)

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

                            if matches.Count = 0 then sb
                            else
                                if not settings.QuietMode then
                                    let matchedPatternSummary =
                                        if isNull renamePattern.Summary then
                                            sprintf "`%s` (no description)" renamePattern.RegexPattern
                                        else
                                            sprintf "\"%s\"" renamePattern.Summary

                                    printer.Debug (sprintf "Rename pattern %s matched × %d." matchedPatternSummary matches.Count)

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
                                                // group 0 is the whole match; we want groups starting at 1
                                                if i + 1 < m.Groups.Count then m.Groups.[i + 1].Value.Trim() else String.Empty
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
                        |> fun p -> p.Normalize(getNormalizationForm settings.NormalizationForm)

                    File.Move(file.FullName, dest)
                    printer.Debug (sprintf "• From: \"%s\"" file.Name)
                    printer.Debug (sprintf "    To: \"%s\"" newFileName)
                with ex ->
                    printer.Error (sprintf "• Error renaming \"%s\": %s" file.Name ex.Message)

            printer.Info (sprintf "Renaming done in %s." watch.ElapsedFriendly)
