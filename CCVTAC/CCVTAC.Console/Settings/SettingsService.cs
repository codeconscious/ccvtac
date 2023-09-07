using System.IO;
using System.Text.Json;

namespace CCVTAC.Console.Settings;

public static class SettingsService
{
    private const string _settingsFileName = "settings.json";

    /// <summary>
    /// A list of audio file format codes used by yt-dlp and that are supported
    /// by TagLib# (for tagging) as well.
    /// </summary>
    public static readonly string[] ValidAudioFormats =
        new string[] {
            // "aac",
            // "flac",
            "m4a", // Recommended for most or all videos since conversation is unnecessary.
            // "mp3",
            // "vorbis",
            // "wav"
        };

    /// <summary>
    /// Prints a summary of the given settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="printer"></param>
    /// <param name="header">An optional line of text to appear above the settings.</param>
    public static void PrintSummary(UserSettings settings, Printer printer, string? header = null)
    {
        if (!string.IsNullOrWhiteSpace(header))
            printer.Print(header);

        printer.Print($"• Downloading {settings.AudioFormat!.ToUpperInvariant()} files");
        printer.Print($"• Video chapters {(settings.SplitChapters ? "WILL" : "will NOT")} be split");
        printer.Print($"• Working directory: {settings.WorkingDirectory}");
        printer.Print($"• Move-to directory: {settings.MoveToDirectory}");
    }

    public static Result<UserSettings> GetUserSettings()
    {
        var readSettingsResult = Read(createFileIfMissing: true);
        if (readSettingsResult.IsFailed)
            return Result.Fail(readSettingsResult.Errors.Select(e => e.Message));

        UserSettings settings = readSettingsResult.Value;

        var ensureValidSettingsResult = EnsureValidSettings(settings);
        if (ensureValidSettingsResult.IsFailed)
            return Result.Fail(ensureValidSettingsResult.Errors.Select(e => e.Message));

        TagFormat.SetId3v2Version(
            version: TagFormat.Id3v2Version.TwoPoint3,
            forceAsDefault: true);

        return readSettingsResult.Value;
    }

    /// <summary>
    /// Creates the specified settings file if it is missing. Otherwise, does nothing.
    /// </summary>
    /// <returns>A Result indicating success or no action (Ok) or else failure (Fail).</returns>
    private static Result CreateIfMissing()
    {
        if (File.Exists(_settingsFileName))
            return Result.Ok();

        try
        {
            return Write(new UserSettings());
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error creating \"{_settingsFileName}\": {ex.Message}");
        }
    }

    /// <summary>
    /// Reads the settings file and parses the JSON to a Settings object.
    /// </summary>
    private static Result<UserSettings> Read(bool createFileIfMissing = false)
    {
        try
        {
            if (createFileIfMissing && CreateIfMissing().IsFailed)
                return Result.Fail($"Settings file \"{_settingsFileName}\" missing.");

            var text = File.ReadAllText(_settingsFileName);
            var settings = JsonSerializer.Deserialize<UserSettings>(text)
                           ?? throw new JsonException();
            return Result.Ok(settings);
        }
        catch (FileNotFoundException)
        {
            return Result.Fail($"Settings file \"{_settingsFileName}\" not found.");
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Settings file JSON is invalid: {ex.Message}");
        }
    }

    /// <summary>
    /// Write settings to the specified file.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="printer"></param>
    /// <returns>A Result indicating success or failure.</returns>
    private static Result Write(UserSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(
                        System.Text.Unicode.UnicodeRanges.All)
                });
            File.WriteAllText(_settingsFileName, json);
            return Result.Ok();
        }
        catch (FileNotFoundException)
        {
            return Result.Fail($"Settings file \"{_settingsFileName}\" is missing.");
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Invalid JSON in settings file: {ex.Message}.");
        }
    }

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

        if (!ValidAudioFormats.Contains(settings.AudioFormat))
        {
            errors.Add(
                $"Invalid audio format in settings: \"{settings.AudioFormat}\". " +
                $"Please use one of the following: \"{string.Join("\", \"", ValidAudioFormats)}\".");
        }

        return errors.Any()
            ? Result.Fail(errors)
            : Result.Ok();
    }
}
