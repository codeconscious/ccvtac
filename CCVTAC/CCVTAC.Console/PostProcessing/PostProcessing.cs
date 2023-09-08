using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using CCVTAC.Console.Settings;

namespace CCVTAC.Console.PostProcessing;

public sealed class Setup
{
    public UserSettings UserSettings { get; }
    public Printer Printer { get; }

    public Setup(UserSettings userSettings, Printer printer)
    {
        UserSettings = userSettings;
        Printer = printer;
    }

    internal void Run()
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        Printer.Print("Starting post-processing...");

        var playlistJsonResult = GetPlaylistJson(UserSettings.WorkingDirectory);
        YouTubePlaylistJson.Root? playlistJson;
        if (playlistJsonResult.IsFailed)
        {
            Printer.Warning($"Error reading playlist JSON file: {playlistJsonResult.Errors.First().Message}");
            playlistJson = null;
        }
        else
        {
            playlistJson = playlistJsonResult.Value;
        }

        // TODO: Create an interface and iterate through them, calling `Run()`?
        ImageProcessor.Run(UserSettings.WorkingDirectory, Printer);
        var tagResult = Tagger.Run(UserSettings, playlistJson, Printer);
        if (tagResult.IsSuccess)
        {
            Printer.Print(tagResult.Value);

            // AudioNormalizer.Run(UserSettings.WorkingDirectory, Printer); // TODO: `mp3gain`は無理なので、別のnormalize方法を要検討。
            Renamer.Run(UserSettings.WorkingDirectory, Printer);
            Deleter.Run(UserSettings.WorkingDirectory, Printer);
            Mover.Run(UserSettings.WorkingDirectory, UserSettings.MoveToDirectory, playlistJson, Printer, true);
        }
        else
        {
            Printer.Errors("Tagging error(s) preventing further post-processing: ", tagResult);
        }

        Printer.Print($"Post-processing done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }

    internal Result<YouTubePlaylistJson.Root> GetPlaylistJson(string workingDirectory)
    {
        var regex = new Regex(@"(?<=\[)[\w\-]{20,}(?=\]\.info.json)"); // Assumes ID length > 20 chars.

        try
        {
            var playlistJsonFileName = Directory.GetFiles(workingDirectory).Single(f => regex.IsMatch(f));
            var playlistJson = File.ReadAllText(playlistJsonFileName);
            var parsedJson = JsonSerializer.Deserialize<YouTubePlaylistJson.Root>(playlistJson);

            if (parsedJson is null)
                return Result.Fail($"The JSON from file \"{playlistJsonFileName}\" was unexpectedly null.");

            return Result.Ok(parsedJson);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error reading or parsing playlist JSON file: {ex.Message}");
        }
    }
}
