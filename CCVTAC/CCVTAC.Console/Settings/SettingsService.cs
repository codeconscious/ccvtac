using System.IO;
using System.Text.Json;
using Spectre.Console;

namespace CCVTAC.Console.Settings;

public class SettingsService(string? customFilePath = null)
{
    private const string _defaultSettingsFileName = "settings.json";
    private string FullPath { get; init; } = customFilePath
                                             ?? Path.Combine(
                                                    AppContext.BaseDirectory,
                                                    _defaultSettingsFileName);

    /// <summary>
    /// Prints a summary of the given settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="printer"></param>
    /// <param name="header">An optional line of text to appear above the settings.</param>
    public static void PrintSummary(UserSettings settings, Printer printer, string? header = null)
    {
        if (header.HasText())
            printer.Print(header!);

        string historyFileNote = File.Exists(settings.HistoryLogFilePath)
            ? "exists"
            : "will be created";

        var settingPairs = new Dictionary<string, string>()
        {
            { $"Audio file format", settings.AudioFormat.ToUpperInvariant() },
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
            { "Working directory", settings.WorkingDirectory },
            { "Move-to directory", settings.MoveToDirectory },
            { "History log file", $"{settings.HistoryLogFilePath} ({historyFileNote})" },
        }.ToImmutableList();

        var table = new Table();
        table.Expand();
        table.Border(TableBorder.HeavyEdge);
        table.BorderColor(Color.Grey27);
        table.AddColumns("Name", "Value");
        table.HideHeaders();
        table.Columns[1].Width = 100; // Ensure its at maximum width.

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

    public Result<UserSettings> GetUserSettings()
    {
        UserSettings settings;
        try
        {
            string json = File.ReadAllText(FullPath);
            settings = JsonSerializer.Deserialize<UserSettings>(json)
                       ?? throw new JsonException();
        }
        catch (FileNotFoundException)
        {
            return Result.Fail($"Settings file \"{FullPath}\" not found.");
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Settings file JSON is invalid: {ex.Message}");
        }

        if (EnsureValidSettings(settings) is { IsFailed: true} failedResult)
            return Result.Fail(failedResult.Errors.Select(e => e.Message));

        // Ensure ID3 version 2.3, which seems more widely supported than version 2.4.
        TagFormat.SetId3v2Version(
            version: TagFormat.Id3v2Version.TwoPoint3,
            forceAsDefault: true);

        return Result.Ok(settings);
    }

    public bool DoesFileExist()
    {
        return File.Exists(FullPath);
    }

    /// <summary>
    /// Creates the specified settings file if it is missing. Otherwise, does nothing.
    /// </summary>
    /// <returns>A Result indicating success or no action (Ok) or else failure (Fail).</returns>
    public Result WriteDefaultFile()
    {
        try
        {
            return Write(new UserSettings());
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error creating \"{FullPath}\": {ex.Message}");
        }
    }

    /// <summary>
    /// Write settings to the specified file.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="printer"></param>
    /// <returns>A Result indicating success or failure.</returns>
    private Result Write(UserSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(
                                 System.Text.Unicode.UnicodeRanges.All)
                });
            File.WriteAllText(FullPath, json);
            return Result.Ok();
        }
        catch (FileNotFoundException)
        {
            return Result.Fail($"Settings file \"{FullPath}\" is missing.");
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Invalid JSON in settings file: {ex.Message}.");
        }
    }

    /// <summary>
    /// Ensure the mandatory settings are present and valid.
    /// </summary>
    /// <returns>A Result indicating success or failure.</returns>
    private static Result EnsureValidSettings(UserSettings settings)
    {
        List<string> errors = new();

        if (settings.MoveToDirectory is null)
            errors.Add($"No move-to directory was specified in the settings.");
        else if (!Directory.Exists(settings.MoveToDirectory))
            errors.Add($"Move-to directory \"{settings.MoveToDirectory}\" does not exist.");

        if (settings.WorkingDirectory is null)
            errors.Add($"No working directory was specified in the settings.");
        else if (!Directory.Exists(settings.WorkingDirectory))
            errors.Add($"Working directory \"{settings.WorkingDirectory}\" does not exist.");

        return errors.Any()
            ? Result.Fail(errors)
            : Result.Ok();
    }
}
