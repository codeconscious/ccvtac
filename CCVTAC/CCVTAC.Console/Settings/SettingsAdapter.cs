using System.IO;
using Spectre.Console;
using CCVTAC.FSharp.Settings;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.Settings;

public class SettingsAdapter
{
    /// <summary>
    /// Reads settings or creates a new default settings file.
    /// </summary>
    /// <param name="maybeSettingsPath"></param>
    /// <param name="printer"></param>
    /// <returns>
    ///     A Result indicating three possible conditions:
    ///     1. `Ok` with successfully parsed user settings from the disk.
    ///     2. `OK` with no value, indicating that a new default file was created.
    ///     3. `Fail`, indicating a failure in the read or write process or in settings validation.
    /// </returns>
    /// <remarks>This is intended to be a temporary solution until more code is moved to F#.</remarks>
    internal static Result<UserSettings> ProcessSettings(string? maybeSettingsPath, Printer printer)
    {
        var path = FilePath.NewFilePath(maybeSettingsPath);

        if (FSharp.Settings.IO.FileExists(path) is { IsOk: true })
        {
            try
            {
                var result = FSharp.Settings.IO.Read(path);
                if (result is { IsError: true })
                    return Result.Fail($"Settings validation error: {result.ErrorValue}");

                return Result.Ok(result.ResultValue);
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error reading settings: {ex.Message}");
            }
        }

        try
        {
            var result = FSharp.Settings.IO.WriteDefaultFile(path);
            if (result is { IsError: true })
                return Result.Fail($"Unexpected error writing the default settings: {result.ErrorValue}");

            printer.Print(result.ResultValue); // The new-file message.
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error writing default settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Prints a summary of the given settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="printer"></param>
    /// <param name="header">An optional line of text to appear above the settings.</param>
    internal static void PrintSummary(
        UserSettings settings,
        Printer printer,
        string? header = null)
    {
        if (header.HasText())
            printer.Print(header!);

        string historyFileNote = File.Exists(settings.HistoryFile)
            ? "exists"
            : "will be created";

        var table = new Table();
        table.Expand();
        table.Border(TableBorder.HeavyEdge);
        table.BorderColor(Color.Grey27);
        table.AddColumns("Name", "Value");
        table.HideHeaders();
        table.Columns[1].Width = 100; // Ensure maximum width.

        var settingPairs =
            new Dictionary<string, string>()
                {
                    { $"Split video chapters", settings.SplitChapters ? "ON" : "OFF" },
                    {
                        $"Sleep between batches",
                        $"{settings.SleepSecondsBetweenBatches} {PluralizeIfNeeded("second", settings.SleepSecondsBetweenBatches)}"
                    },
                    {
                        $"Sleep between downloads",
                        $"{settings.SleepSecondsBetweenDownloads} {PluralizeIfNeeded("second", settings.SleepSecondsBetweenDownloads)}"
                    },
                    {
                        $"Ignore-upload-year channels",
                        $"{settings.IgnoreUploadYearUploaders?.Length.ToString() ?? "No"} {PluralizeIfNeeded("channel", settings.IgnoreUploadYearUploaders?.Length ?? 0)}"
                    },
                    { "Verbose mode", settings.VerboseOutput ? "ON" : "OFF" },
                    { "Working directory", settings.WorkingDirectory ?? "None found" }, // TODO: Add validation, then remove the null case.
                    { "Move-to directory", settings.MoveToDirectory ?? "None found" },
                    { "History log file", $"{settings.HistoryFile?? "None found" } ({historyFileNote})" },
                }
            .ToImmutableList();
        settingPairs.ForEach(pair => table.AddRow(pair.Key, pair.Value));

        printer.Print(table);

        static string PluralizeIfNeeded(string singularTerm, int count)
        {
            return (singularTerm, count) switch
            {
                { singularTerm: "second", count: 1 } => singularTerm,
                { singularTerm: "second", count: _ } => "seconds",
                { singularTerm: "channel", count: 1 } => singularTerm,
                { singularTerm: "channel", count: _ } => "channels",
                _ => singularTerm
            };
        }
    }
}
