namespace CCVTAC.Console;

sealed class ResultTracker
{
    private nuint _successCount;
    private readonly Dictionary<string, string> _failures = [];
    private readonly Printer _printer;

    public ResultTracker(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        _printer = printer;
    }

    public void RegisterResult(string url, Result<string> result)
    {
        if (result.IsSuccess)
        {
            _successCount++;

            if (result.Value.HasText())
                _printer.Info(result.Value);
        }
        else
        {
            _printer.Errors(result);

            _failures.Add(url, result.Value);
        }
    }

    public void PrintSummary()
    {
        string successLabel = _successCount == 1 ? "success" : "successes";
        string failureLabel = _failures.Count == 1 ? "failure" : "failures";

        _printer.Info($"Quitting with {_successCount} {successLabel} and {_failures.Count} {failureLabel}.");

        foreach (var (url, error) in _failures)
        {
            _printer.Warning($"- {url}: {error}");
        }
    }
}
