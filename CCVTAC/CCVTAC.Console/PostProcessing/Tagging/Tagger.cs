using System.Diagnostics;
using System.IO;
using System.Text.Json;
using CCVTAC.Console.Settings;

namespace CCVTAC.Console.PostProcessing.Tagging;

internal static class Tagger
{
    internal static Result<string> Run(UserSettings userSettings,
                                       YouTubeCollectionJson.Root? collectionJson,
                                       Printer printer)
    {
        printer.Print("Adding file tags...");

        Stopwatch stopwatch = new();
        stopwatch.Start();

        var taggingSetsResult = GenerateTaggingSets(userSettings.WorkingDirectory);
        if (taggingSetsResult.IsFailed)
        {
            return Result.Fail("No tagging sets were generated, so tagging cannot be done.");
        }

        foreach (TaggingSet taggingSet in taggingSetsResult.Value)
        {
            ProcessSingleTaggingSet(userSettings, taggingSet, collectionJson, printer);
        }

        return Result.Ok($"Tagging done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }

    private static Result<ImmutableList<TaggingSet>> GenerateTaggingSets(string directory)
    {
        try
        {
            string[] allFiles = Directory.GetFiles(directory);
            ImmutableList<TaggingSet> taggingSets = TaggingSet.CreateTaggingSets(allFiles); // TODO: Refactor? 名がこのメソッドと被りすぎている。

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

    private static void ProcessSingleTaggingSet(
        UserSettings userSettings,
        TaggingSet taggingSet,
        YouTubeCollectionJson.Root? collectionJson,
        Printer printer)
    {
        printer.Print($"{taggingSet.AudioFilePaths.Count()} audio file(s) with resource ID \"{taggingSet.ResourceId}\"");

        var parsedJsonResult = GetParsedJson(taggingSet);
        if (parsedJsonResult.IsFailed)
        {
            printer.Errors(parsedJsonResult);
            return;
        }

        TaggingSet confirmedTaggingSet = DeleteSourceFile(taggingSet, printer);

        foreach (string audioFilePath in confirmedTaggingSet.AudioFilePaths)
        {
            TagSingleFile(
                userSettings,
                parsedJsonResult.Value,
                audioFilePath,
                taggingSet.ImageFilePath,
                collectionJson,
                printer);
        }
    }

    static void TagSingleFile(UserSettings                userSettings,
                              YouTubeVideoJson.Root       parsedVideoJson,
                              string                      audioFilePath,
                              string                      imageFilePath,
                              YouTubeCollectionJson.Root? collectionJson,
                              Printer                     printer)
    {
        try
        {
            string audioFileName = Path.GetFileName(audioFilePath);
            printer.Print($"Current audio file: \"{audioFileName}\"");

            using var taggedFile = TagLib.File.Create(audioFilePath);
            TagDetector tagDetector = new();

            if (parsedVideoJson.Track is string officialTitle)
            {
                printer.Print($"• Using official title \"{officialTitle}\"");
                taggedFile.Tag.Title = officialTitle;
            }
            else
            {
                taggedFile.Tag.Title = tagDetector.DetectTitle(parsedVideoJson, parsedVideoJson.Title);
                printer.Print($"• Found title \"{taggedFile.Tag.Title}\"");
            }

            if (parsedVideoJson.Artist is string officialArtist)
            {
                printer.Print($"• Using official artist \"{officialArtist}\"");
                taggedFile.Tag.Performers = new[] { officialArtist };
            }
            else if (tagDetector.DetectArtist(parsedVideoJson) is string artist)
            {
                printer.Print($"• Found artist \"{artist}\"");
                taggedFile.Tag.Performers = new[] { artist };
            }

            if (parsedVideoJson.Album is string officialAlbum)
            {
                printer.Print($"• Using official album \"{officialAlbum}\"");
                taggedFile.Tag.Album = officialAlbum;
            }
            else if (tagDetector.DetectAlbum(parsedVideoJson, collectionJson?.Title) is string album)
            {
                printer.Print($"• Found album \"{album}\"");
                taggedFile.Tag.Album = album;
            }

            if (tagDetector.DetectComposers(parsedVideoJson) is string composers)
            {
                printer.Print($"• Found composer(s) \"{composers}\"");
                taggedFile.Tag.Composers = new[] { composers };
            }

            if (parsedVideoJson.PlaylistIndex is uint trackNo)
            {
                printer.Print($"• Using playlist index of {trackNo} for track number");
                taggedFile.Tag.Track = trackNo;
            }

            if (parsedVideoJson.ReleaseYear is uint releaseYear)
            {
                printer.Print($"• Using official release year \"{releaseYear}\"");
                taggedFile.Tag.Year = releaseYear;
            }
            else
            {
                ushort? defaultYear = userSettings.GetVideoUploadDateIfRegisteredUploader(parsedVideoJson);
                if (defaultYear is not null)
                {
                    printer.Print($"Will use upload year {defaultYear} for uploader \"{parsedVideoJson.Uploader}\" if no other year is detected.");
                }

                if (tagDetector.DetectReleaseYear(parsedVideoJson, defaultYear) is ushort year)
                {
                    printer.Print($"• Found year \"{year}\"");
                    taggedFile.Tag.Year = year;
                }
            }

            taggedFile.Tag.Comment = parsedVideoJson.GenerateComment(collectionJson);

            WriteImage(taggedFile, imageFilePath, printer);

            taggedFile.Save();
            printer.Print($"Wrote tags to \"{audioFileName}\".");
        }
        catch (Exception ex)
        {
            printer.Error($"Error tagging file: {ex.Message}");
        }
    }

    static Result<YouTubeVideoJson.Root> GetParsedJson(TaggingSet taggingSet)
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

        YouTubeVideoJson.Root? parsedJson;
        try
        {
            parsedJson = JsonSerializer.Deserialize<YouTubeVideoJson.Root>(json);

            if (parsedJson is null)
            {
                return Result.Fail($"The JSON from file \"{taggingSet.JsonFilePath}\" was unexpectedly null.");
            }

            return Result.Ok(parsedJson.Value);
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Error deserializing JSON from file \"{taggingSet.JsonFilePath}\": {ex.Message}");
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
        {
            return taggingSet;
        }

        FileInfo largestFileInfo =
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
