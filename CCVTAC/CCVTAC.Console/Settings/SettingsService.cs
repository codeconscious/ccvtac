using System.IO;
using Spectre.Console;
using CCVTAC.FSharp.Settings;
using FSettings = CCVTAC.FSharp.Settings.UserSettings; // "F" for F# -- a temporary contrivance for clarity

namespace CCVTAC.Console.Settings;

public class SettingsService
{
    private const string _defaultFileName = "settings.json";

    private string FullPath { get; init; }

    internal SettingsService(string? customFilePath = null)
    {
        FullPath = customFilePath
                   ?? Path.Combine(AppContext.BaseDirectory, _defaultFileName);
    }

    internal bool FileExists() => File.Exists(FullPath);

    /// <summary>
    /// Read the settings from the specified JSON file.
    /// </summary>
    /// <exception cref="InvalidOperationException">Indicates IO or validation errors.</exception>
    internal FSettings Read()
    {
        var path = FilePath.NewFilePath(FullPath);
        var result = IO.Read(path);
        if (result.IsError)
            throw new InvalidOperationException(result.ErrorValue);

        return result.ResultValue;
    }

    /// <summary>
    /// Creates the specified settings file if it is missing. Otherwise, does nothing.
    /// </summary>
    /// <exception cref="InvalidOperationException">Indicates an IO error.</exception>
    internal Result WriteDefault()
    {
        var path = FilePath.NewFilePath(FullPath);
        var result = IO.WriteDefaultFile(path);
        return result.IsOk
            ? Result.Ok()
            : throw new InvalidOperationException(result.ErrorValue);
    }

    /// <summary>
    /// Prints a summary of the given settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="printer"></param>
    /// <param name="header">An optional line of text to appear above the settings.</param>
    internal void PrintSummary(
        FSettings settings,
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
