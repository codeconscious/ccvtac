using CCVTAC.Console.Settings;

namespace CCVTAC.Console;

internal static class Program
{
    private static readonly string[] HelpCommands = new[] { "-h", "--help" };
    private static readonly string[] QuitCommands = new[] { "q", "quit", "exit", "bye" };
    private static readonly string InputPrompt = "Enter a YouTube URL (or 'q' to quit):";

    static void Main(string[] args)
    {
        var printer = new Printer();

        if (args.Length > 0 && HelpCommands.Contains(args[0].ToLowerInvariant()))
        {
            Help.Print(printer);
            return;
        }

        var settingsResult = GetSettings();
        if (settingsResult.IsFailed)
        {
            printer.Errors(settingsResult.Errors.Select(e => e.Message), "Settings file errors:");
            return;
        }
        var settings = settingsResult.Value;
        SettingsService.PrintSummary(settings, printer, "Settings loaded OK:");

        TagFormat.SetId3v2Version(
            version: TagFormat.Id3v2Version.TwoPoint3,
            forceAsDefault: true);

        var resultCounter = new ResultHandler(printer);
        while (true)
        {
            if (!Run(settings, resultCounter, printer))
                break;
        }

        resultCounter.PrintFinalSummary();
    }

    private static bool Run(UserSettings settings, ResultHandler resultHandler, Printer printer)
    {
        string userInput = printer.GetInput(InputPrompt);

        if (QuitCommands.Contains(userInput.ToLowerInvariant()))
        {
            return false;
        }

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var downloadResult = Downloading.Downloader.Run(userInput, settings, printer);
        resultHandler.RegisterResult(downloadResult);

        History.Append(userInput, printer);

        var postProcessor = new PostProcessing.Setup(settings, printer);
        postProcessor.Run();

        printer.Print($"Done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
        return true;
    }

    private sealed class ResultHandler
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
                    _printer.Errors(messages, "Download error(s):");
            }
        }

        public void PrintFinalSummary()
        {
            var successLabel = _successCount == 1 ? "success" : "successes";
            var failureLabel = _failureCount == 1 ? "failure" : "failures";
            _printer.Print($"Quitting with {_successCount} {successLabel} and {_failureCount} {failureLabel}.");
        }
    }

    static Result<UserSettings> GetSettings()
    {
        var readSettingsResult = SettingsService.Read(createFileIfMissing: true);
        if (readSettingsResult.IsFailed)
            return Result.Fail(readSettingsResult.Errors.Select(e => e.Message));

        UserSettings settings = readSettingsResult.Value;

        var ensureValidSettingsResult = SettingsService.EnsureValidSettings(settings);
        if (ensureValidSettingsResult.IsFailed)
        {
            return ensureValidSettingsResult;
        }

        return readSettingsResult.Value;
    }
}
