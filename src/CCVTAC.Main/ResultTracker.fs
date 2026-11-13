namespace CCVTAC.Main

open CCFSharpUtils
open CCFSharpUtils.Text
open System
open System.Collections.Generic

type ResultTracker(printer: Printer) =

    let mutable successCount : uint64 = 0UL

    let failures = Dictionary<string, string>()

    /// Logs the result for a specific corresponding input.
    member _.RegisterResult<'T>(input: string, result: Result<'T, string list>) : unit =
        match result with
        | Ok _ ->
            successCount <- successCount + 1UL
        | Error e ->
            let msg = match e with [] -> String.Empty | _ -> List.head e
            if not (failures.TryAdd(input, msg)) then
                failures[input] <- msg


    /// Prints any failures for the current batch.
    member _.PrintBatchFailures() : unit =
        if Num.isZero failures.Count then
            printer.Debug "No failures in batch."
        else
            let failureSummary = String.plur' "failure" S failures.Count
            printer.Info $"%s{failureSummary} in this batch:"
            for pair in failures do
                printer.Warning $"- %s{pair.Key}: %s{pair.Value}"

    /// Prints the output for the current application session.
    member _.PrintSessionSummary() : unit =
        let successSummary = String.plur' "success" Es successCount
        let failureSummary = String.plur' "failure" S failures.Count

        printer.Info $"Quitting with %s{successSummary} and %s{failureSummary}."

        for pair in failures do
            printer.Warning $"- %s{pair.Key}: %s{pair.Value}"
