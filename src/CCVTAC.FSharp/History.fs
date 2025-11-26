namespace CCVTAC.Console

open System
open System.IO
open System.Text.Json
open Spectre.Console

type History(filePath: string, displayCount: byte) =

    let separator = ';'

    member private _.FilePath = filePath
    member private _.DisplayCount = displayCount

    /// Add a URL and related data to the history file.
    member this.Append(url: string, entryTime: DateTime, printer: Printer) : unit =
        try
            let serializedEntryTime = JsonSerializer.Serialize(entryTime).Replace("\"", "")
            File.AppendAllText(this.FilePath, serializedEntryTime + string separator + url + Environment.NewLine)
            printer.Debug (sprintf "Added \"%s\" to the history log." url)
        with ex ->
            printer.Error ("Could not append URL(s) to history log: " + ex.Message)

    member this.ShowRecent(printer: Printer) : unit =
        try
            // Read lines and take the last N lines in the original order
            let max = int this.DisplayCount
            let lines =
                File.ReadAllLines(this.FilePath)
                |> Seq.rev
                |> Seq.truncate max
                |> Seq.rev
                |> Seq.toList

            let historyData =
                lines
                |> Seq.map _.Split(separator)
                |> Seq.filter (fun parts -> parts.Length = 2)
                |> Seq.map (fun parts -> DateTime.Parse(parts[0]), parts[1])
                |> Seq.groupBy fst
                |> Seq.map (fun (dt, pairs) -> dt, pairs |> Seq.map snd |> Seq.toList)

            let table = Table()
            table.Border <- TableBorder.None
            table.AddColumns("Time", "URL") |> ignore
            table.Columns[0].PadRight(3) |> ignore

            for (dateTime, urls) in historyData do
                let formattedTime = sprintf "%s" (dateTime.ToString("yyyy-MM-dd HH:mm:ss"))
                let joinedUrls = String.Join(Environment.NewLine, urls)
                table.AddRow(formattedTime, joinedUrls) |> ignore

            Printer.PrintTable table
        with ex ->
            printer.Error (sprintf "Could not display recent history: %s" ex.Message)
