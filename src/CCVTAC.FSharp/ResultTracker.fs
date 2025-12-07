namespace CCVTAC.Console

open System
open System.Collections.Generic

type ResultTracker<'a>(printer: Printer) =

    let mutable successCount : uint64 = 0UL

    let failures = Dictionary<string,string>()

    let _printer = printer

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

    /// Prints any failures for the current batch.
    member _.PrintBatchFailures() : unit =
        if isZero failures.Count then
            _printer.Debug("No failures in batch.")
        else
            let failureLabel = if failures.Count = 1 then "failure" else "failures"
            _printer.Info $"%d{failures.Count} %s{failureLabel} in this batch:"
            for kvp in failures do
                _printer.Warning $"- %s{kvp.Key}: %s{kvp.Value}"

    /// Prints the output for the current application session.
    member _.PrintSessionSummary() : unit =
        let successLabel = if successCount = 1UL then "success" else "successes"
        let failureLabel = if failures.Count = 1 then "failure" else "failures"

        _printer.Info $"Quitting with %d{successCount} %s{successLabel} and %d{failures.Count} %s{failureLabel}."

        for kvp in failures do
            _printer.Warning $"- %s{kvp.Key}: %s{kvp.Value}"
