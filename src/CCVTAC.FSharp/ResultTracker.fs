namespace CCVTAC.Console

open System
open System.Collections.Generic
open System.Linq

type ResultTracker<'T>(printer: Printer) =

    let mutable successCount : uint64 = 0UL
    let failures = Dictionary<string,string>()
    let _printer =
        if isNull (box printer) then nullArg "printer" else printer

    static let combineErrors (result: Result<'T>) =
        String.Join(" / ", result.Errors.Select(fun e -> e.Message))

    /// Logs the result for a specific corresponding input.
    member _.RegisterResult(input: string, result: Result<'T>) : unit =
        if result.IsSuccess then
            successCount <- successCount + 1UL
        else
            let errors = combineErrors result
            if not (failures.TryAdd(input, errors)) then
                // Keep only the latest error for each input.
                failures.[input] <- errors

    /// Prints any failures for the current batch.
    member _.PrintBatchFailures() : unit =
        if failures.Count = 0 then
            _printer.Debug("No failures in batch.")
        else
            let failureLabel = if failures.Count = 1 then "failure" else "failures"
            _printer.Info(sprintf "%d %s in this batch:" failures.Count failureLabel)
            for kvp in failures do
                _printer.Warning(sprintf "- %s: %s" kvp.Key kvp.Value)

    /// Prints the output for the current application session.
    member _.PrintSessionSummary() : unit =
        let successLabel = if successCount = 1UL then "success" else "successes"
        let failureLabel = if failures.Count = 1 then "failure" else "failures"

        _printer.Info(sprintf "Quitting with %d %s and %d %s." successCount successLabel failures.Count failureLabel)

        for kvp in failures do
            _printer.Warning(sprintf "- %s: %s" kvp.Key kvp.Value)
