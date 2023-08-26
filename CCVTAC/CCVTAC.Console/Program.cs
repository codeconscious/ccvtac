using CCVTAC.Console.Settings;

namespace CCVTAC.Console;

class Program
{
    private static readonly string[] HelpCommands = new[] { "-h", "--help" };
    private static readonly string[] QuitCommands = new[] { "q", "quit", "exit", "bye" };

    static void Main(string[] args)
    {
        var printer = new Printer();

        if (args.Length > 0 && HelpCommands.Contains(args[0].ToLowerInvariant()))
        {
            Help.PrintHelp(printer);
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

        const string prompt = "Enter a YouTube URL (or 'q' to quit): ";
        nuint successCount = 0;
        nuint failureCount = 0;
        while (true)
        {
            string input = printer.GetInput(prompt);

            if (QuitCommands.Contains(input.ToLowerInvariant()))
            {
                ShowResults(successCount, failureCount, printer);
                return;
            }

            var downloadResult = Downloading.Downloader.Run(input, settings, printer);
            if (downloadResult.IsSuccess)
            {
                successCount++;
                printer.Print(downloadResult.Value);
            }
            else
            {
                failureCount++;
                printer.Errors(
                    downloadResult.Errors.Select(e => e.Message),
                    "Download errors:");
            }

            History.Append(input, printer);

            var postProcessor = new PostProcessing.Setup(settings, printer);
            postProcessor.Run();
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

    static void ShowResults(nuint successCount, nuint failureCount, Printer printer)
    {
        var successLabel = successCount == 1 ? "success" : "successes";
        var failureLabel = failureCount == 1 ? "failure" : "failures";
        printer.Print($"Quitting with {successCount} {successLabel} and {failureCount} {failureLabel} for downloads.");
        return;
    }
}
