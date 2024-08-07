namespace CCVTAC.Console;

sealed class ResultTracker
{
    private nuint _successCount;
    private nuint _failureCount;
    private readonly Printer _printer;

    public ResultTracker(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        _printer = printer;
    }

    public void RegisterResult(Result<string> downloadResult)
    {
        if (downloadResult.IsSuccess)
        {
            _successCount++;

            if (downloadResult.Value.HasText())
                _printer.Info(downloadResult.Value);
        }
        else
        {
            _failureCount++;

            if (downloadResult.IsFailed)
                _printer.Errors(downloadResult);
        }
    }

    public void PrintSummary()
    {
        string successLabel = _successCount == 1 ? "success" : "successes";
        string failureLabel = _failureCount == 1 ? "failure" : "failures";

        _printer.Info($"Quitting with {_successCount} {successLabel} and {_failureCount} {failureLabel}.");
    }
}
