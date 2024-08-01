using CCVTAC.Console.Settings;
using CCVTAC.Console.Downloading;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console;

internal static class Program
{
    private static readonly string[] _helpFlags = ["-h", "--help"];
    private static readonly string[] _settingsFileFlags = ["-s", "--settings"];
    private const string _defaultSettingsFileName = "settings.json";

    static void Main(string[] args)
    {
        Printer printer = new();

        if (args.Length > 0 && _helpFlags.CaseInsensitiveContains(args[0]))
        {
            Help.Print(printer);
            return;
        }

        string? maybeSettingsPath = args.Length >= 2 &&
                                    _settingsFileFlags.CaseInsensitiveContains(args[0])
                ? args[1] // Expected to be a settings file path.
                : _defaultSettingsFileName;

        var settingsResult = SettingsAdapter.ProcessSettings(maybeSettingsPath, printer);
        if (settingsResult.IsFailed)
        {
            printer.Errors(settingsResult.Errors.Select(e => e.Message));
            return;
        }
        else if (settingsResult.Value is null) // If a new settings files was created.
        {
            return;
        }
        var settings = settingsResult.Value;
        SettingsAdapter.PrintSummary(settings, printer, header: "Settings loaded OK.");

        // Catch Ctrl-C (SIGINT).
        System.Console.CancelKeyPress += delegate
        {
            printer.Warning("\nQuitting at user's request.");

            var tempFiles = IoUtilties.Directories.GetDirectoryFileNames(settings.WorkingDirectory);
            if (tempFiles.Any())
            {
                printer.Error($"Please clean up the {tempFiles.Count} file(s) leftover in the working directory ({settings.WorkingDirectory}):");
                tempFiles.ForEach(file => printer.Warning($"• {file}"));
            }
        };

        // Top-level `try` block to catch and pretty-print unexpected exceptions.
        try
        {
            Orchestrator.Start(settings, printer);
        }
        catch (Exception topException)
        {
            printer.Error($"Fatal error: {topException.Message}");
            AnsiConsole.WriteException(topException);
            printer.Print("Please help improve this tool by reporting this error and any relevant URLs at https://github.com/codeconscious/ccvtac/issues.");
        }
    }
}
