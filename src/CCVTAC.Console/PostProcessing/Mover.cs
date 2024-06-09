using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using CCVTAC.Console.PostProcessing.Tagging;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.PostProcessing;

internal static class Mover
{
    private static readonly Regex _playlistImageRegex = new(@"\[[OP]L[\w\d_-]+\]"); // TODO: Add channels.

    internal static void Run(
        IEnumerable<TaggingSet> taggingSets,
        CollectionMetadata? maybeCollectionData,
        UserSettings settings,
        bool overwrite,
        Printer printer)
    {
        Watch watch = new();

        bool verbose = settings.VerboseOutput;
        DirectoryInfo workingDirInfo = new(settings.WorkingDirectory);

        string subFolderName = GetSafeSubDirectoryName(maybeCollectionData, taggingSets.First());
        string collectionName = maybeCollectionData?.Title ?? string.Empty;
        string fullMoveToDir = Path.Combine(settings.MoveToDirectory, subFolderName, collectionName);

        var dirResult = EnsureDirectoryExists(fullMoveToDir, verbose, printer);
        if (dirResult.IsFailed)
        {
            return;
        }

        var audioFileNames = workingDirInfo.EnumerateFiles("*.m4a").ToList();
        if (audioFileNames.IsEmpty())
        {
            printer.Error("No audio filenames were found.");
            return;
        }

        if (verbose)
        {
            printer.Print($"Moving {audioFileNames.Count} audio file(s) to \"{fullMoveToDir}\"...");
        }

        var (successCount, failureCount) =
            MoveAudioFiles(audioFileNames, fullMoveToDir, overwrite, verbose, printer);

        MoveImageFile(collectionName, subFolderName, workingDirInfo, fullMoveToDir, audioFileNames.Count, printer);

        var fileLabel = successCount == 1 ? "file" : "files";
        printer.Print($"Moved {successCount} audio {fileLabel} in {watch.ElapsedFriendly}.");

        if (failureCount > 0)
        {
            fileLabel = failureCount == 1 ? "file": "files";
            printer.Warning($"However, {failureCount} audio {fileLabel} could not be moved.");
        }
    }

    private static bool IsPlaylistImage(string fileName)
    {
        return _playlistImageRegex.IsMatch(fileName);
    }

    private static FileInfo? GetCoverImage(DirectoryInfo workingDirInfo, int audioFileCount)
    {
        var images = workingDirInfo.EnumerateFiles("*.jpg").ToImmutableArray();
        if (images.IsEmpty())
            return null;

        var playlistImages = images.Where(i => IsPlaylistImage(i.FullName));
        if (playlistImages.Any())
            return playlistImages.First();

        return audioFileCount > 1 && images.Length == 1
            ? images.First()
            : null;
    }

    private static Result EnsureDirectoryExists(string moveToDir, bool verbose, Printer printer)
    {
        try
        {
            if (Path.Exists(moveToDir))
            {
                if (verbose)
                    printer.Print($"Found move-to directory \"{moveToDir}\".");

                return Result.Ok();
            }

            if (verbose)
            {
                printer.Print($"Creating move-to directory \"{moveToDir}\" (based on playlist metadata)... ",
                              appendLineBreak: false);
            }

            Directory.CreateDirectory(moveToDir);

            printer.Print($"Created move-to directory \"{moveToDir}\".");
            return Result.Ok();

        }
        catch (Exception ex)
        {
            printer.Error($"Error creating move-to directory \"{moveToDir}\": {ex.Message}");
            return Result.Fail(string.Empty);
        }
    }

    private static (uint successCount, uint failureCount) MoveAudioFiles(
        ICollection<FileInfo> audioFiles,
        string moveToDir,
        bool overwrite,
        bool verbose,
        Printer printer)
    {
        uint successCount = 0;
        uint failureCount = 0;

        foreach (FileInfo file in audioFiles)
        {
            try
            {
                File.Move(
                    file.FullName,
                    Path.Combine(moveToDir, file.Name),
                    overwrite);

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

        return (successCount, failureCount);
    }

    private static void MoveImageFile(
        string maybeCollectionName,
        string subFolderName,
        DirectoryInfo workingDirInfo,
        string moveToDir,
        int audioFileCount,
        Printer printer)
    {
        try
        {
            var baseFileName = string.IsNullOrWhiteSpace(maybeCollectionName)
                ? subFolderName
                : $"{subFolderName} - {maybeCollectionName}";

            if (GetCoverImage(workingDirInfo, audioFileCount) is FileInfo image)
            {
                image.MoveTo(
                    Path.Combine(moveToDir, $"{baseFileName.Trim()}.jpg"),
                    overwrite: false);

                printer.Print("Moved image file.");
            }
        }
        catch (Exception ex)
        {
            printer.Warning($"Failed to copy the image file: {ex.Message}");
        }
    }

    private static string GetSafeSubDirectoryName(CollectionMetadata? maybeCollectionData, TaggingSet taggingSet)
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
