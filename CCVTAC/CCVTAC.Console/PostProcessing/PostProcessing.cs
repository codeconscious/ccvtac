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

        var jsonResult = GetCollectionJson(UserSettings.WorkingDirectory);
        YouTubeCollectionJson.Root? collectionJson;
        if (jsonResult.IsFailed)
        {
            Printer.Print($"No playlist or channel data loaded: {jsonResult.Errors.First().Message}");
            collectionJson = null;
        }
        else
        {
            collectionJson = jsonResult.Value;
        }

        ImageProcessor.Run(UserSettings.WorkingDirectory, Printer);

        var tagResult = Tagger.Run(UserSettings, collectionJson, Printer);
        if (tagResult.IsSuccess)
        {
            Printer.Print(tagResult.Value);

            // AudioNormalizer.Run(UserSettings.WorkingDirectory, Printer); // TODO: `mp3gain`は無理なので、別のnormalize方法を要検討。
            Renamer.Run(UserSettings.WorkingDirectory, Printer);
            Deleter.Run(UserSettings.WorkingDirectory, Printer);
            Mover.Run(UserSettings.WorkingDirectory, UserSettings.MoveToDirectory, collectionJson, true, Printer);
        }
        else
        {
            Printer.Errors("Tagging error(s) preventing further post-processing: ", tagResult);
        }

        Printer.Print($"Post-processing done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }

    internal Result<YouTubeCollectionJson.Root> GetCollectionJson(string workingDirectory)
    {
        var regex = new Regex(@"(?<=\[)[\w\-]{20,}(?=\]\.info.json)"); // Assumes ID length > 20 chars.

        try
        {
            var fileNames = Directory.GetFiles(workingDirectory)
                                     .Where(f => regex.IsMatch(f))
                                     .ToImmutableList();

            if (fileNames.Count == 0)
                return Result.Fail("No relevant files found (so this is likely not a playlist download).");
            if (fileNames.Count > 1)
                return Result.Fail("Unexpectedly found more than 1 relevant file, so none will be processed.");
            var fileName = fileNames.Single();

            var json = File.ReadAllText(fileName);
            var parsedJson = JsonSerializer.Deserialize<YouTubeCollectionJson.Root>(json);

            if (parsedJson is null)
                return Result.Fail($"The parsed JSON from file \"{fileName}\" was unexpectedly null.");

            return Result.Ok(parsedJson);
        }
        catch (Exception ex)
        {
            return Result.Fail($"{ex.Message}");
        }
    }
}
