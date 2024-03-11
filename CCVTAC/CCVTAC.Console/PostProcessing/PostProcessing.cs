using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using CCVTAC.Console.Settings;
using CCVTAC.Console.PostProcessing.Tagging;
using FSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.PostProcessing;

public sealed class Setup(FSettings userSettings, Printer printer)
{
    public FSettings UserSettings { get; } = userSettings;
    public Printer Printer { get; } = printer;

    internal void Run()
    {
        if (UserSettings.PauseBeforePostProcessing)
        {
            Printer.Warning("Paused before post processing. Press the Enter key to continue.");
            System.Console.ReadLine();
        }

        Watch watch = new();

        Printer.Print("Starting post-processing...");

        var taggingSetsResult = GenerateTaggingSets(UserSettings.WorkingDirectory);
        if (taggingSetsResult.IsFailed)
        {
            Printer.Error("No tagging sets were generated, so tagging cannot be done.");
            return;
        }
        var taggingSets = taggingSetsResult.Value;

        var collectionJsonResult = GetCollectionJson(UserSettings.WorkingDirectory);
        CollectionMetadata? collectionJson;
        if (collectionJsonResult.IsFailed)
        {
            Printer.Print($"No playlist or channel metadata found: {collectionJsonResult.Errors.First().Message}");
            collectionJson = null;
        }
        else
        {
            collectionJson = collectionJsonResult.Value;
        }

        ImageProcessor.Run(UserSettings.WorkingDirectory, Printer);

        var tagResult = Tagger.Run(UserSettings, taggingSets, collectionJson, Printer);
        if (tagResult.IsSuccess)
        {
            Printer.Print(tagResult.Value);

            // AudioNormalizer.Run(UserSettings.WorkingDirectory, Printer); // TODO: `mp3gain`は無理なので、別のnormalize方法を要検討。
            Renamer.Run(UserSettings.WorkingDirectory, UserSettings.VerboseOutput, Printer);
            Mover.Run(taggingSets, collectionJson, UserSettings, true, Printer);
            Deleter.Run(UserSettings.WorkingDirectory, UserSettings.VerboseOutput, Printer);
        }
        else
        {
            Printer.Errors("Tagging error(s) preventing further post-processing: ", tagResult);
        }

        Printer.Print($"Post-processing done in {watch.ElapsedFriendly}.");
    }

    internal Result<CollectionMetadata> GetCollectionJson(string workingDirectory)
    {
        Regex regex = new("""(?<=\[)[\w\-]{17,}(?=\]\.info.json)"""); // Assumes a minimum ID length.

        try
        {
            var fileNames = Directory.GetFiles(workingDirectory)
                                     .Where(f => regex.IsMatch(f))
                                     .ToFrozenSet();

            if (fileNames.Count == 0)
            {
                return Result.Fail("No relevant files found.");
            }

            if (fileNames.Count > 1)
            {
                return Result.Fail("Unexpectedly found more than one relevant file, so none will be processed.");
            }

            string fileName = fileNames.Single();
            string json = File.ReadAllText(fileName);
            CollectionMetadata collectionData = JsonSerializer.Deserialize<CollectionMetadata>(json);
            return Result.Ok(collectionData);
        }
        catch (Exception ex)
        {
            return Result.Fail($"{ex.Message}");
        }
    }

    private static Result<ImmutableList<TaggingSet>> GenerateTaggingSets(string directory)
    {
        try
        {
            string[] files = Directory.GetFiles(directory);
            ImmutableList<TaggingSet> taggingSets = TaggingSet.CreateSets(files);

            return taggingSets is not null && taggingSets.Any()
                ? Result.Ok(taggingSets)
                : Result.Fail($"No tagging sets were created using working directory \"{directory}\".");
        }
        catch (DirectoryNotFoundException)
        {
            return Result.Fail($"Directory \"{directory}\" does not exist.");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error reading working directory files: {ex.Message}");
        }
    }
}
