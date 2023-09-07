using System.IO;
using System.Text.Json;
using CCVTAC.Console.Settings;

namespace CCVTAC.Console.PostProcessing;

internal static class Tagger
{
    internal static Result<string> Run(UserSettings userSettings, Printer printer)
    {
        printer.Print("Adding file tags...");

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var taggingSetsResult = GetTaggingSets(userSettings.WorkingDirectory);
        if (taggingSetsResult.IsFailed)
            return Result.Fail("No tagging sets were generated, so tagging cannot be done.");

        foreach (var taggingSet in taggingSetsResult.Value)
        {
            ProcessSingleTaggingSet(userSettings, taggingSet, printer);
        }

        return Result.Ok($"Tagging done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }

    private static Result<ImmutableList<TaggingSet>> GetTaggingSets(string workingDirectory)
    {
        try
        {
            var allFiles = Directory.GetFiles(workingDirectory);
            var taggingSets = TaggingSet.CreateTaggingSets(allFiles);

            return taggingSets is not null && taggingSets.Any()
                ? Result.Ok(taggingSets)
                : Result.Fail("No tagging sets were created.");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error reading working directory files: {ex.Message}");
        }
    }

    static void ProcessSingleTaggingSet(UserSettings userSettings, TaggingSet taggingSet, Printer printer)
    {
        printer.Print($"{taggingSet.AudioFilePaths.Count()} audio file(s) with resource ID \"{taggingSet.ResourceId}\"");

        var parsedJsonResult = GetParsedJson(taggingSet);
        if (parsedJsonResult.IsFailed)
        {
            printer.Errors(parsedJsonResult);
            return;
        }

        var confirmedTaggingSet = DeleteSourceFile(taggingSet, printer);

        foreach (var audioFilePath in confirmedTaggingSet.AudioFilePaths)
        {
            TagSingleFile(userSettings, parsedJsonResult.Value, audioFilePath, taggingSet.ImageFilePath, printer);
        }
    }

    static void TagSingleFile(
        UserSettings userSettings,
        YouTubeJson.Root parsedJson,
        string audioFilePath,
        string imageFilePath,
        Printer printer)
    {
        try
        {
            var audioFileName = Path.GetFileName(audioFilePath);
            printer.Print($"Current audio file: \"{audioFileName}\"");

            using var taggedFile = TagLib.File.Create(audioFilePath);
            var tagDetector = new TagDetector();
            taggedFile.Tag.Title = tagDetector.DetectTitle(parsedJson, printer, parsedJson.title);

            if (tagDetector.DetectArtist(parsedJson, printer) is string artist)
            {
                taggedFile.Tag.Performers = new[] { artist };
            }

            if (tagDetector.DetectAlbum(parsedJson, printer) is string album)
            {
                taggedFile.Tag.Album = album;
            }

            ushort? defaultYear =
                userSettings.UseUploadYearForUploaders?.ContainsCaseInsensitive(parsedJson.uploader) == true &&
                ushort.TryParse(parsedJson.upload_date[0..4], out var parsedYear)
                    ? parsedYear
                    : null;
            if (tagDetector.DetectReleaseYear(parsedJson, printer, defaultYear) is ushort year)
            {
                taggedFile.Tag.Year = year;
            }

            taggedFile.Tag.Comment = parsedJson.GenerateComment();

            if (tagDetector.DetectComposer(parsedJson, printer) is string composer)
            {
                taggedFile.Tag.Composers = new[] { composer };
            }

            WriteImage(taggedFile, imageFilePath, printer);

            taggedFile.Save();
            printer.Print($"Wrote tags to \"{audioFileName}\".");
        }
        catch (Exception ex)
        {
            printer.Error($"Error tagging file: {ex.Message}");
        }
    }

    static Result<YouTubeJson.Root> GetParsedJson(TaggingSet taggingSet)
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

        YouTubeJson.Root? parsedJson;
        try
        {
            parsedJson = JsonSerializer.Deserialize<YouTubeJson.Root>(json);

            if (parsedJson is null)
            {
                return Result.Fail($"The JSON from file \"{taggingSet.JsonFilePath}\" was unexpectedly null.");
            }

            return Result.Ok(parsedJson.Value);
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Error deserializing JSON from file \"{json}\": {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes the pre-split, source audio (if any) for split videos, each of which will have the same resource ID.
    /// </summary>
    /// <param name="taggingSet"></param>
    /// <param name="printer"></param>
    private static TaggingSet DeleteSourceFile(TaggingSet taggingSet, Printer printer)
    {
        // If there is only one file, then there are no child files, so no action is necessary.
        // (Two files should never happen, but might be worth thinking about how to handle that.)
        if (taggingSet.AudioFilePaths.Count() <= 1)
            return taggingSet;

        var largestFileInfo =
            taggingSet.AudioFilePaths
                .Select(fn => new FileInfo(fn))
                .OrderByDescending(fi => fi.Length)
                .First();

        try
        {
            File.Delete(largestFileInfo.FullName);
            printer.Print($"Deleted pre-split source file \"{largestFileInfo.Name}\"");
            return taggingSet with { AudioFilePaths = taggingSet.AudioFilePaths.Remove(largestFileInfo.FullName) };
        }
        catch (Exception ex)
        {
            printer.Error($"Error deleting pre-split source file \"{largestFileInfo.Name}\": {ex.Message}");
            return taggingSet;
        }
    }

    /// <summary>
    /// Write the video thumbnail to the file tags.
    /// </summary>
    /// <param name="taggedFile"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="printer"></param> <summary>
    /// <remarks>Heavily inspired by https://stackoverflow.com/a/61264720/11767771.</remarks>
    private static void WriteImage(TagLib.File taggedFile, string imageFilePath, Printer printer)
    {
        if (string.IsNullOrWhiteSpace(imageFilePath))
        {
            printer.Error("No image file path was provided, so cannot add an image to the file.");
            return;
        }

        try
        {
            var pics = new TagLib.IPicture[1];
            pics[0] = new TagLib.Picture(imageFilePath);
            taggedFile.Tag.Pictures = pics;
            printer.Print("Image written to file tags OK.");
        }
        catch (Exception ex)
        {
            printer.Error($"Error writing image to the audio file: {ex.Message}");
            return;
        }
    }
}
