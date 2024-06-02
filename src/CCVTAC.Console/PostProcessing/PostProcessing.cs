using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using CCVTAC.Console.PostProcessing.Tagging;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;
using static CCVTAC.FSharp.Downloading;

namespace CCVTAC.Console.PostProcessing;

public sealed class PostProcessing
{
    public UserSettings Settings { get; }
    public Printer Printer { get; }
    public MediaType MediaType { get; }

    public PostProcessing(UserSettings settings, MediaType mediaType, Printer printer)
    {
        Settings = settings;
        Printer = printer;
        MediaType = mediaType;
    }

    internal void Run()
    {
        Watch watch = new();

        Printer.Print("Starting post-processing...");

        var taggingSetsResult = GenerateTaggingSets(Settings.WorkingDirectory);
        if (taggingSetsResult.IsFailed)
        {
            Printer.Error("No tagging sets were generated, so tagging cannot be done.");
            return;
        }
        var taggingSets = taggingSetsResult.Value;

        var collectionJsonResult = GetCollectionJson(Settings.WorkingDirectory);
        CollectionMetadata? collectionJson;
        if (collectionJsonResult.IsFailed)
        {
            if (Settings.VerboseOutput)
                Printer.Print($"No playlist or channel metadata found: {collectionJsonResult.Errors.First().Message}");

            collectionJson = null;
        }
        else
        {
            collectionJson = collectionJsonResult.Value;
        }

        if (Settings.EmbedImages)
        {
            ImageProcessor.Run(Settings.WorkingDirectory, Settings.VerboseOutput, Printer);
        }

        var tagResult = Tagger.Run(Settings, taggingSets, collectionJson, MediaType, Printer);
        if (tagResult.IsSuccess)
        {
            Printer.Print(tagResult.Value);

            // AudioNormalizer.Run(UserSettings.WorkingDirectory, Printer); // TODO: normalize方法を要検討。
            Renamer.Run(Settings.WorkingDirectory, Settings.VerboseOutput, Printer);
            Mover.Run(taggingSets, collectionJson, Settings, true, Printer);

            var taggingSetFileNames = taggingSets.SelectMany(set => set.AllFiles).ToImmutableList();
            Deleter.Run(taggingSetFileNames, Settings.VerboseOutput, Printer);
            Deleter.CheckRemaining(Settings.WorkingDirectory, Printer);
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
