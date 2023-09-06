using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing;

internal static class Tagger
{
    internal static Result<string> Run(string workingDirectory, Printer printer)
    {
        printer.Print("Adding file tags...");

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var taggingSetsResult = GetTaggingSets(workingDirectory);
        if (taggingSetsResult.IsFailed)
            return Result.Fail("No tagging sets were generated, so tagging cannot be done.");

        foreach (var taggingSet in taggingSetsResult.Value)
        {
            ProcessSingleTaggingSet(taggingSet, printer);
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

    static void ProcessSingleTaggingSet(TaggingSet taggingSet, Printer printer)
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
            TagSingleFile(parsedJsonResult.Value, audioFilePath, taggingSet.ImageFilePath, printer);
        }
    }

    static void TagSingleFile(
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
            taggedFile.Tag.Title = DetectTitle(parsedJson, printer, parsedJson.title);
            if (DetectArtist(parsedJson, printer) is string artist)
            {
                taggedFile.Tag.Performers = new[] { artist };
            }
            if (DetectAlbum(parsedJson, printer) is string album)
            {
                taggedFile.Tag.Album = album;
            }
            if (DetectReleaseYear(parsedJson, printer, 0) is ushort year)
            {
                taggedFile.Tag.Year = year;
            }
            taggedFile.Tag.Comment = parsedJson.GenerateComment();

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

    static string? DetectTitle(YouTubeJson.Root data, Printer printer, string? defaultName = null)
    {
        // TODO: Put this somewhere where it can be static.
        List<(string Regex, int Group, string Text, string Source)> parsePatterns = new()
        {
            (
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
                1,
                data.description,
                "description (Topic style)"
            ),
        };

        foreach (var pattern in parsePatterns)
        {
            var regex = new Regex(pattern.Regex);
            var match = regex.Match(pattern.Text);

            if (!match.Success)
                continue;

            printer.Print($"Writing title \"{match.Groups[pattern.Group].Value}\" (matched via {pattern.Source})");
            return match.Groups[pattern.Group].Value.Trim();
        }

        printer.Print($"Writing title \"{defaultName}\" (taken from video title)");
        return defaultName;
    }

    static string? DetectArtist(YouTubeJson.Root data, Printer printer, string? defaultName = null)
    {
        // TODO: Put this somewhere where it can be static.
        List<(string Regex, int Group, string Text, string Source)> parsePatterns = new()
        {
            (
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
                2,
                data.description,
                "description (Topic style)"
            ),
        };

        foreach (var pattern in parsePatterns)
        {
            var regex = new Regex(pattern.Regex);
            var match = regex.Match(pattern.Text);

            if (!match.Success)
                continue;

            printer.Print($"Writing artist \"{match.Groups[pattern.Group].Value}\" (matched via {pattern.Source})");
            return match.Groups[pattern.Group].Value.Trim();
        }

        return defaultName;
    }

    static string? DetectAlbum(YouTubeJson.Root data, Printer printer, string? defaultName = null)
    {
        // TODO: Put this somewhere where it can be static or else a setting.
        List<(string Regex, int Group, string Text, string Source)> parsePatterns = new()
        {
            (
                @"(?<=[Aa]lbum: ).+",
                0,
                data.description,
                "description"
            ),
            (
                @"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D",
                3,
                data.description,
                "description (Topic style)"
            ),
            (
                """(?<='s ['"]).+(?=['"] album)""",
                0,
                data.description,
                "description"
            ),
        };

        foreach (var pattern in parsePatterns)
        {
            var regex = new Regex(pattern.Regex);
            var match = regex.Match(pattern.Text);

            if (!match.Success)
                continue;

            printer.Print($"Writing album \"{match.Groups[pattern.Group].Value}\" (matched via {pattern.Source})");
            return match.Groups[pattern.Group].Value.Trim();
        }

        return defaultName;
    }

    /// <summary>
    /// Attempt to automatically detect a release year in the video metadata.
    /// If none is found, return a default value.
    /// </summary>
    static ushort? DetectReleaseYear(YouTubeJson.Root data, Printer printer, ushort? defaultYear = null)
    {
        // TODO: Put this somewhere where it can be static or made a setting.
        List<(string Regex, string Text, string Source)> parsePatterns = new()
        {
            (
                @"(?<=[(（\[［【])[12]\d{3}(?=[)）\]］】])",
                data.title,
                "title"
            ),
            (
                @"(?<=℗ )[12]\d{3}(?=\s)",
                data.description,
                "description's \"℗\" symbol"
            ),
            (
                @"(?<=[Rr]eleased [io]n: )[12]\d{3}",
                data.description,
                "description 'released on' date"
            ),
            (
                @"[12]\d{3}(?=年(?:\d{1,2}月\d{1,2}日)?リリース)",
                data.description,
                "description's リリース-style date"
            ),
            (
                @"[12]\d{3}年(?=\d{1,2}月\d{1,2}日\s?[Rr]elease)",
                data.description,
                "description's 年月日-style release date"
            ),
        };

        foreach (var pattern in parsePatterns)
        {
            var result = ParseYear(pattern.Regex, pattern.Text);
            if (result is null)
                continue;

            printer.Print($"Writing year {result.Value} (matched via {pattern.Source})");
            return result.Value;
        }

        printer.Print($"No year could be parsed{(defaultYear is null ? "." : $", so defaulting to {defaultYear}.")}");
        return defaultYear;

        /// <summary>
        /// Applies a regex pattern against text, returning the matched value
        /// or else null if there was no successful match.
        /// </summary>
        /// <param name="regexPattern"></param>
        /// <param name="text">Text that might contain a year.</param>
        /// <returns>A number representing a year or null.</returns>
        static ushort? ParseYear(string regexPattern, string text)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(regexPattern);

            var regex = new Regex(regexPattern);
            var match = regex.Match(text);

            if (match is null)
                return null;
            return ushort.TryParse(match.Value, out var matchYear)
                ? matchYear
                : null;
        };
    }

    /// <summary>
    /// Deletes the pre-split, source audio (if any) for split videos, each of which will have the same resource ID.
    /// </summary>
    /// <param name="taggingSet"></param>
    /// <param name="printer"></param>
    private static TaggingSet DeleteSourceFile(TaggingSet taggingSet, Printer printer)
    {
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
