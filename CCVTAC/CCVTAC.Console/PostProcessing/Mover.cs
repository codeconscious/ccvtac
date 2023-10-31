using System.Diagnostics;
using System.IO;
using System.Text.Json;
using CCVTAC.Console.PostProcessing.Tagging;

namespace CCVTAC.Console.PostProcessing;

internal static class Mover
{
    internal static void Run(string workingDirectory,
                             string moveToDirectory,
                             IEnumerable<TaggingSet> taggingSets,
                             CollectionMetadata? maybeCollectionData,
                             bool shouldOverwrite,
                             Printer printer)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        uint successCount = 0;
        uint failureCount = 0;
        DirectoryInfo workingDirInfo = new(workingDirectory);

        string subFolderName = GetDefaultFolderName(maybeCollectionData, taggingSets.First(), workingDirInfo);
        string moveToDir = Path.Combine(moveToDirectory, subFolderName);

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

        printer.Print($"Moving audio file(s) to \"{moveToDir}\"...");
        foreach (FileInfo file in workingDirInfo.EnumerateFiles("*.m4a"))
        {
            try
            {
                File.Move(
                    file.FullName,
                    $"{Path.Combine(moveToDir, file.Name)}",
                    shouldOverwrite);
                successCount++;
                printer.Print($"• Moved \"{file.Name}\"");
            }
            catch (Exception ex)
            {
                failureCount++;
                printer.Error($"• Error moving file \"{file.Name}\": {ex.Message}");
            }
        }

        printer.Print($"{successCount} file(s) moved in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
        if (failureCount > 0)
        {
            printer.Warning($"However, {failureCount} file(s) could not be moved.");
        }
    }

    private static string GetDefaultFolderName(CollectionMetadata? maybeCollectionData, TaggingSet taggingSet, DirectoryInfo workingDirInfo)
    {
        string workingName;

        if (maybeCollectionData is CollectionMetadata collectionData &&
            !string.IsNullOrWhiteSpace(collectionData.Uploader) &&
            !string.IsNullOrWhiteSpace(collectionData.Title))
        {
            workingName = $"{collectionData.Uploader} - {collectionData.Title}";
        }
        else
        {
            var jsonResult = GetParsedVideoJson(taggingSet);
            workingName = jsonResult.IsSuccess
                ? jsonResult.Value.Uploader
                : string.Empty;
        }

        return workingName.ReplaceInvalidPathChars();
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
