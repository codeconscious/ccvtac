namespace CCVTAC.Console;

sealed class ResultHandler
{
    private nuint _successCount;
    private nuint _failureCount;
    private readonly Printer _printer;

    public ResultHandler(Printer printer)
    {
        ArgumentNullException.ThrowIfNull(printer);
        _printer = printer;
    }

    public void RegisterResult(Result<string> downloadResult)
    {
        if (downloadResult.IsSuccess)
        {
            _successCount++;

            if (!string.IsNullOrWhiteSpace(downloadResult?.Value))
                _printer.Print(downloadResult.Value);
        }
        else
        {
            _failureCount++;

            var messages = downloadResult.Errors.Select(e => e.Message);
            if (messages?.Any() == true)
                _printer.Errors("Download error(s):", messages);
        }
    }

    public void PrintFinalSummary()
    {
        var successLabel = _successCount == 1 ? "success" : "successes";
        var failureLabel = _failureCount == 1 ? "failure" : "failures";
        _printer.Print($"Quitting with {_successCount} {successLabel} and {_failureCount} {failureLabel}.");
    }
}
