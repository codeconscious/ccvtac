namespace CCVTAC.Main

open CCFSharpUtils
open CCFSharpUtils.Text
open System
open System.Collections.Generic

type ResultTracker<'a>(printer: Printer) =

    let mutable successCount : uint64 = 0UL

    let failures = Dictionary<string,string>()

    static let combineErrors (errors: string list) =
        String.Join(" / ", errors)

    member _.AddSuccess() : unit =
        successCount <- successCount + 1UL

    member _.AddFailure(url: string, error: string) : unit =
        failures.Add(url, error)

    /// Logs the result for a specific corresponding input.
    member _.RegisterResult(input: string, result: Result<'a, string>) : unit =
        match result with
        | Ok _ ->
            successCount <- successCount + 1UL
        | Error e ->
            if not (failures.TryAdd(input, e)) then
                failures[input] <- e

    /// Logs the result for a specific corresponding input.
    member _.RegisterResult(input: string, result: Result<'a, string list>) : unit =
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
        printer.Debug $"successCount   == {successCount}"
        printer.Debug $"failures.Count == {failures.Count}"

        let successSummary = String.plur' "success" Es successCount
        let failureSummary = String.plur' "failure" S failures.Count

        printer.Info $"Quitting with %s{successSummary} and %s{failureSummary}."

        for pair in failures do
            printer.Warning $"- %s{pair.Key}: %s{pair.Value}"
