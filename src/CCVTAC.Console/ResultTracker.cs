namespace CCVTAC.Console;

internal sealed class ResultTracker<T>
{
    private nuint _successCount;
    private readonly Dictionary<string, string> _failures = [];
    private readonly Printer _printer;

    private static string CombinedErrors(Result<T> result) =>
        string.Join(" / ", result.Errors.Select(e => e.Message));

    public ResultTracker(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        _printer = printer;
    }

    public void RegisterResult(string input, Result<T> result)
    {
        if (result.IsSuccess)
        {
            _successCount++;
            return;
        }

        var errors = CombinedErrors(result);
        if (!_failures.TryAdd(input, errors))
        {
            // Keep the latest error for a specific input.
            _failures[input] = errors;
        }
    }

    public void PrintBatchFailures()
    {
        if (_failures.Count == 0)
        {
            _printer.Debug("No failures in batch.");
            return;
        }

        var failureLabel = _failures.Count == 1 ? "failure" : "failures";

        _printer.Info($"{_failures.Count} {failureLabel} in this batch:");

        foreach (var (url, error) in _failures)
        {
            _printer.Warning($"- {url}: {error}");
        }
    }

    public void PrintSessionSummary()
    {
        var successLabel = _successCount == 1 ? "success" : "successes";
        var failureLabel = _failures.Count == 1 ? "failure" : "failures";

        _printer.Info($"Quitting with {_successCount} {successLabel} and {_failures.Count} {failureLabel}.");

        foreach (var (url, error) in _failures)
        {
            _printer.Warning($"- {url}: {error}");
        }
    }
}
