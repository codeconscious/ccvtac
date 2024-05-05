using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using CCVTAC.Console.PostProcessing.Tagging;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.PostProcessing;

internal static class Mover
{
    private static readonly string _coverImageFileName = "cover.jpg";

    private static bool IsPlaylistImage(string fileName)
    {
        var regex = new Regex(@"\[[OP]L[\w\d_-]+\]");
        return regex.IsMatch(fileName);
    }

    private static FileInfo? GetCoverImage(DirectoryInfo workingDirInfo, int audioFileCount)
    {
        var images = workingDirInfo.EnumerateFiles("*.jpg").ToImmutableArray();
        if (images.IsEmpty())
            return null;

        var playlistImages = images.Where(i => IsPlaylistImage(i.FullName));
        if (playlistImages.Any())
            return playlistImages.First();

        if (audioFileCount > 1 && images.Length == 1)
            return images.First();
        else
            return null;
    }

    internal static void Run(
        IEnumerable<TaggingSet> taggingSets,
        CollectionMetadata? maybeCollectionData,
        UserSettings settings,
        bool shouldOverwrite,
        Printer printer)
    {
        Watch watch = new();

        uint successCount = 0;
        uint failureCount = 0;
        bool verbose = settings.VerboseOutput;
        DirectoryInfo workingDirInfo = new(settings.WorkingDirectory);

        string subFolderName = GetDefaultDirectoryName(maybeCollectionData, taggingSets.First());
        string collectionFolder = maybeCollectionData?.Title ?? string.Empty;
        string moveToDir = Path.Combine(settings.MoveToDirectory, subFolderName, collectionFolder);

        try
        {
            if (!Path.Exists(moveToDir))
            {
                printer.Print($"Creating move-to directory \"{moveToDir}\" (based on playlist metadata)... ",
                              appendLineBreak: false);
                Directory.CreateDirectory(moveToDir);
                printer.Print("OK!");
            }
        }
        catch (Exception ex)
        {
            printer.Error($"Error creating move-to directory \"{moveToDir}\": {ex.Message}");
            throw;
        }

        var audioFiles = workingDirInfo.EnumerateFiles("*.m4a").ToList();
        printer.Print($"Moving {audioFiles.Count} audio file(s) to \"{moveToDir}\"...");

        foreach (FileInfo file in audioFiles)
        {
            try
            {
                File.Move(
                    file.FullName,
                    $"{Path.Combine(moveToDir, file.Name)}",
                    shouldOverwrite);

                successCount++;

                if (verbose)
                    printer.Print($"• Moved \"{file.Name}\"");
            }
            catch (Exception ex)
            {
                failureCount++;
                printer.Error($"• Error moving file \"{file.Name}\": {ex.Message}");
            }
        }

        try
        {
            if (GetCoverImage(workingDirInfo, audioFiles.Count) is FileInfo image)
            {
                image.MoveTo(
                    Path.Combine(moveToDir, _coverImageFileName),
                    overwrite: false);
            }

        }
        catch (Exception ex)
        {
            printer.Warning($"Failed to copy the image file: {ex.Message}");
        }

        printer.Print($"{successCount} audio file(s) moved in {watch.ElapsedFriendly}.");
        if (failureCount > 0)
        {
            printer.Warning($"However, {failureCount} audio file(s) could not be moved.");
        }
    }

    private static string GetDefaultDirectoryName(CollectionMetadata? maybeCollectionData, TaggingSet taggingSet)
    {
        string workingName;

        if (maybeCollectionData is CollectionMetadata collectionData &&
            collectionData.Uploader.HasText() &&
            collectionData.Title.HasText())
        {
            workingName = $"{collectionData.Uploader}";
        }
        else
        {
            var jsonResult = GetParsedVideoJson(taggingSet);
            workingName = jsonResult.IsSuccess
                ? jsonResult.Value.Uploader
                : string.Empty;
        }

        return workingName.ReplaceInvalidPathChars().Trim();
    }

    private static Result<VideoMetadata> GetParsedVideoJson(TaggingSet taggingSet)
    {
        string json;
        try
        {
            json = File.ReadAllText(taggingSet.JsonFilePath);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error reading JSON file \"{taggingSet.JsonFilePath}\": {ex.Message}.");
        }

        VideoMetadata videoData;
        try
        {
            videoData = JsonSerializer.Deserialize<VideoMetadata>(json);
            return Result.Ok(videoData);
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Error deserializing JSON from file \"{taggingSet.JsonFilePath}\": {ex.Message}");
        }
    }
}
