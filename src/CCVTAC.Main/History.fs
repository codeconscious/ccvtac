namespace CCVTAC.Main

open CCVTAC.Main.IoUtilities.Files
open System
open System.IO
open System.Text.Json
open Spectre.Console

type History(filePath: string, displayCount: int) =

    let separator = ';'

    member private _.FilePath = filePath
    member private _.DisplayCount = displayCount

    /// Write a URL and its related data to the history file.
    member this.Append(url: string, entryTime: DateTime, printer: Printer) : unit =
        try
            let serializedTime = JsonSerializer.Serialize(entryTime).Replace("\"", "")
            let text = serializedTime + string separator + url + String.newLine

            match appendToFile this.FilePath text with
            | Ok _      -> printer.Debug $"Added \"%s{url}\" to the history log."
            | Error err -> printer.Error $"Failed to write \"%s{url}\" to the history log at \"{this.FilePath}\": {err}"
        with exn ->
            printer.Error $"Could not append URL(s) to history log: {exn.Message}"

    member this.ShowRecent(printer: Printer) : unit =
        try
            let lines =
                File.ReadAllLines this.FilePath
                |> Seq.rev
                |> Seq.truncate this.DisplayCount
                |> Seq.rev
                |> Seq.toList

            let historyData =
                lines
                |> Seq.choose (fun line ->
                    match line.Split separator with
                    | [| dateText; url |] ->
                        match DateTime.TryParse dateText with
                        | true, date -> Some (date, url)
                        | _ -> None
                    | _ -> None)
                |> Seq.groupBy fst
                |> Seq.map (fun (dt, pairs) -> dt, pairs |> Seq.map snd |> Seq.toList)

            // TODO: These presentation matters shouldn't be here.
            let table = Table()
            table.Border <- TableBorder.None
            table.AddColumns("Time", "URL") |> ignore
            table.Columns[0].PadRight(3) |> ignore

            for dateTime, urls in historyData do
                let formattedTime = sprintf "%s" (dateTime.ToString("yyyy-MM-dd HH:mm:ss"))
                let joinedUrls = String.Join(String.newLine, urls)
                table.AddRow(formattedTime, joinedUrls) |> ignore

            Printer.PrintTable table
        with exn ->
            printer.Error $"Could not display history: %s{exn.Message}"
