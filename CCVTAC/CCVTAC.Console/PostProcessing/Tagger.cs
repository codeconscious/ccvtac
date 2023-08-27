using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing;

internal static class Tagger
{
    internal static void Run(string workingDirectory, Printer printer)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        const string jsonFileSearchPattern = "*.json";
        List<string> jsonFiles;
        try
        {
            jsonFiles = Directory.GetFiles(workingDirectory, jsonFileSearchPattern).ToList();
            if (!jsonFiles.Any())
            {
                printer.Error($"No {jsonFileSearchPattern} files found! Aborting...");
                return;
            }
        }
        catch (Exception ex)
        {
            printer.Error($"Error reading {jsonFileSearchPattern} files: " + ex.Message);
            return;
        }

        var resourceIdRegex = new Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)");
        var idsWithFileNames = jsonFiles
                        .Select(f => resourceIdRegex.Match(f))
                        .Where(m => m.Success)
                        .Select(m => m.Captures.OfType<Match>().First())
                                      .GroupBy(m => m.Groups[1].Value,
                                               m => m.Groups[0].Value);
        var invalid = idsWithFileNames.Where(g => g.Count() != 1);
        if (invalid.Any())
        {
            printer.Errors(
                "JSON errors:",
                invalid.Select(i => $"Too many JSON files for ID {i.Key}, so this ID will be skipped.")
            );
        }
        var valid = idsWithFileNames.Where(g => g.Count() == 1);

        foreach (var idNamePair in valid)
        {
            var resourceId = idNamePair.Key;
            var jsonFileName = idNamePair.Single();

            string json;
            try
            {
                json = File.ReadAllText(jsonFileName.Trim());
            }
            catch (Exception ex)
            {
                printer.Error($"Error reading JSON file \"{jsonFileName}\": {ex.Message}.");
                continue;
            }

            YouTubeJson.Root? parsedJson;
            try
            {
                parsedJson = JsonSerializer.Deserialize<YouTubeJson.Root>(json);

                if (parsedJson is null)
                {
                    printer.Error($"JSON from file \"{json}\" was unexpectedly null.");
                    continue;
                }
            }
            catch (JsonException ex)
            {
                printer.Error($"Error deserializing JSON from file \"{json}\": {ex.Message}");
                continue;
            }

            var audioFilesForThisID = Directory
                .GetFiles(workingDirectory)
                .Where(f => Settings.SettingsService.ValidAudioFormats.Any(f.ToLowerInvariant().EndsWith))
                .ToImmutableList();
            if (!audioFilesForThisID.Any())
            {
                printer.Error($"No supported audio files for ID {idNamePair.Key} were found.");
                continue;
            }
            printer.Print($"Found {audioFilesForThisID.Count()} audio file(s) for resource ID \"{idNamePair.Key}\"");

            // For split videos, delete the source file.
            if (audioFilesForThisID.Count() > 1)
            {
                var largestFileInfo = audioFilesForThisID
                    .Select(fn => new FileInfo(fn))
                    .OrderByDescending(fi => fi.Length)
                    .First();

                try
                {
                    File.Delete(largestFileInfo.FullName);
                    audioFilesForThisID.Remove(largestFileInfo.FullName);
                    printer.Print($"Deleted original file \"{largestFileInfo.Name}\"");
                }
                catch (Exception ex)
                {
                    printer.Error($"Error deleting file \"{largestFileInfo.Name}\": {ex.Message}");
                }
            }

            foreach (var audioFilePath in audioFilesForThisID)
            {
                var audioFileName = Path.GetFileName(audioFilePath);
                printer.Print($"Current audio file: \"{audioFileName}\"");

                using var taggedFile = TagLib.File.Create(audioFilePath);
                taggedFile.Tag.Title = parsedJson.title;
                taggedFile.Tag.Year = DetectReleaseYear(parsedJson, printer);
                taggedFile.Tag.Comment = GenerateComment(parsedJson);
                AddImage(taggedFile, resourceId, workingDirectory, printer);
                taggedFile.Save();
                printer.Print($"Wrote tags to \"{audioFileName}\"");
            }
        }

        printer.Print($"Tagging done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");

        /// <summary>
        /// Generate a comment using data parsed from the JSON file.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>The formatted comment.</returns>
        static string GenerateComment(YouTubeJson.Root data)
        {
            StringBuilder sb = new();
            sb.AppendLine("SOURCE DATA:");
            sb.AppendLine($"• Downloaded: {DateTime.Now} using CCVTAC");
            sb.AppendLine($"• Service: {data.extractor_key}");
            sb.AppendLine($"• URL: {data.webpage_url}");
            sb.AppendLine($"• Title: {data.fulltitle}");
            sb.AppendLine($"• Uploader: {data.uploader} ({data.uploader_url})");
            sb.AppendLine($"• Uploaded: {data.upload_date[4..6]}/{data.upload_date[6..8]}/{data.upload_date[0..4]}"); // "08/27/2023"
            sb.AppendLine($"• Description: {data.description})");
            return sb.ToString();
        }

        /// <summary>
        /// Attempt to automatically detect a release year in the video metadata.
        /// If none is found, return a default value.
        /// </summary>
        static uint DetectReleaseYear(YouTubeJson.Root data, Printer printer, ushort defaultYear = 0)
        {
            // TODO: Put this somewhere where it can be static.
            List<(string Regex, string Text, string Source)> parsePatterns = new()
            {
                (@"(?<=[(（\[［【])[12]\d{3}(?=[)）\]］】])", data.title, "title"),
                (@"(?<=℗ )[12]\d{3}(?=\s)", data.description, "description's \"℗\" symbol"),
                (@"(?<=[Rr]eleased [io]n: )[12]\d{3}", data.description, "description 'released on' date"),
                (@"[12]\d{3}(?=年(?:\d{1,2}月\d{1,2}日)?リリース)", data.description, "description's リリース date"),
            };

            foreach (var pattern in parsePatterns)
            {
                var result = ParseYear(pattern.Regex, pattern.Text);
                if (result is null)
                    continue;

                printer.Print($"Writing year {result.Value} (matched via {pattern.Source})");
                return result.Value;
            }

            printer.Print($"No year could be parsed, so defaulting to {defaultYear}.");
            return 0;

            /// <summary>
            /// Applies a regex pattern against text, returning the matched value
            /// or else null if there was no successful match.
            /// </summary>
            /// <param name="regexPattern"></param>
            /// <param name="text">Text that might contain a year.</param>
            /// <returns>A number representing a year or null.</returns>
            static uint? ParseYear(string regexPattern, string text)
            {
                ArgumentNullException.ThrowIfNullOrEmpty(regexPattern);

                var regex = new Regex(regexPattern);
                var match = regex.Match(text);

                if (match is null)
                    return null;
                return uint.TryParse(match.Value, out var matchYear)
                    ? matchYear
                    : null;
            };
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="taggedFile"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="printer"></param> <summary>
    /// <remarks>Heavily inspired by https://stackoverflow.com/a/61264720/11767771.</remarks>
    private static void AddImage(TagLib.File taggedFile, string resourceId, string workingDirectory, Printer printer)
    {
        string imageFile;
        try
        {
            imageFile = Directory.GetFiles(workingDirectory, $"*{resourceId}*.jpg").Single();
        }
        catch (Exception ex)
        {
            printer.Error($"Error finding image file in \"{workingDirectory}\": {ex.Message}");
            printer.Print("Aborting image addition.");
            return;
        }

        try
        {
            var pics = new TagLib.IPicture[1];
            pics[0] = new TagLib.Picture(imageFile);
            taggedFile.Tag.Pictures = pics;
            printer.Print("Image attached to file tags.");
        }
        catch (Exception ex)
        {
            printer.Error($"Error attaching image to the audio file: {ex.Message}");
            printer.Print("Aborting image addition.");
            return;
        }
    }

    /// <summary>
    /// Subversions of ID3 version 2 (such as 2.3 or 2.4).
    /// </summary>
    public enum Id3v2Version : byte
    {
        /// <summary>
        /// Rarely, if ever, used. Not recommended.
        /// </summary>
        TwoPoint2 = 2,

        /// <summary>
        /// The most widely used and supported version. Highly recommended.
        /// </summary>
        TwoPoint3 = 3,

        /// <summary>
        /// The newest version, but is not often used or supported. Not particularly recommended.
        /// </summary>
        TwoPoint4 = 4,
    }
}
