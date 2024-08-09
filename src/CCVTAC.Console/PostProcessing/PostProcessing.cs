using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using CCVTAC.Console.PostProcessing.Tagging;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;
using static CCVTAC.FSharp.Downloading;

namespace CCVTAC.Console.PostProcessing;

public sealed partial class PostProcessing
{
    private UserSettings Settings { get; }
    private MediaType MediaType { get; }
    private Printer Printer { get; }

    public PostProcessing(UserSettings settings, MediaType mediaType, Printer printer)
    {
        (Settings, MediaType, Printer) = (settings, mediaType, printer);
    }

    internal void Run()
    {
        Watch watch = new();
        string workingDirectory = Settings.WorkingDirectory;

        Printer.Info("Starting post-processing...");

        var taggingSetsResult = GenerateTaggingSets(workingDirectory);
        if (taggingSetsResult.IsFailed)
        {
            Printer.Error("No tagging sets were generated, so tagging cannot be done.");
            return;
        }
        var taggingSets = taggingSetsResult.Value;

        var collectionJsonResult = GetCollectionJson(workingDirectory);
        CollectionMetadata? collectionJson;
        if (collectionJsonResult.IsFailed)
        {
            Printer.Debug($"No playlist or channel metadata found: {collectionJsonResult.Errors.First().Message}");
            collectionJson = null;
        }
        else
        {
            Printer.Debug("Found playlist/channel metadata.");
            collectionJson = collectionJsonResult.Value;
        }

        if (Settings.EmbedImages)
        {
            ImageProcessor.Run(workingDirectory, Printer);
        }

        var tagResult = Tagger.Run(Settings, taggingSets, collectionJson, MediaType, Printer);
        if (tagResult.IsSuccess)
        {
            Printer.Info(tagResult.Value);

            // AudioNormalizer.Run(workingDirectory, Printer); // TODO: normalize方法を要検討。
            Renamer.Run(Settings, workingDirectory, Printer);

            Mover.Run(taggingSets, collectionJson, Settings, true, Printer);

            var taggingSetFileNames = taggingSets.SelectMany(set => set.AllFiles).ToList();
            Deleter.Run(taggingSetFileNames, collectionJson, workingDirectory, Printer);

            IoUtilties.Directories.WarnIfAnyFiles(workingDirectory, 10);
        }
        else
        {
            Printer.Errors("Tagging error(s) preventing further post-processing: ", tagResult);
        }

        Printer.Info($"Post-processing done in {watch.ElapsedFriendly}.");
    }

    internal Result<CollectionMetadata> GetCollectionJson(string workingDirectory)
    {
        try
        {
            var fileNames = Directory.GetFiles(workingDirectory)
                                     .Where(f => CollectionMetadataRegex().IsMatch(f))
                                     .ToImmutableHashSet();

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
            var collectionData = JsonSerializer.Deserialize<CollectionMetadata>(json);

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
            var taggingSets = TaggingSet.CreateSets(files);

            return taggingSets?.Any() == true
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

    /// <summary>
    /// A regular expression that detects metadata files for collections.
    /// </summary>
    [GeneratedRegex("""(?<=\[)[\w\-]{17,}(?=\]\.info.json)""")]
    private static partial Regex CollectionMetadataRegex();
}
