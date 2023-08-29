using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CCVTAC.Console.PostProcessing;

internal static class Tagger
{
    private const string _jsonFileExtension = ".json";
    private const string _audioFileExtension = ".m4a"; // TODO: Need to handle more
    private static readonly Regex _videoResourceIdRegex = new(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)");

    internal static Result<string> Run(string workingDirectory, Printer printer)
    {
        printer.Print("Adding file tags...");

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        IReadOnlyList<TaggingSet> taggingSets;
        try
        {
            var allFiles = Directory.GetFiles(workingDirectory);
            taggingSets = CreateTagSets(allFiles, workingDirectory);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error reading {_audioFileExtension} files: " + ex.Message);
        }

        // if (!taggingSets.Any()) // Debugging use only
        // {
        //     var allFiles = Directory.GetFiles(workingDirectory);
        //     printer.Print("Current working files:");
        //     allFiles.ToList().ForEach(f => printer.Print($"- {f}"));

        //     return Result.Fail($"No tagging sets were created! Aborting after {stopwatch.ElapsedMilliseconds:#,##0}ms");
        // }

        foreach (var taggingSet in taggingSets)
        {
            printer.Print($"{taggingSet.AudioFilePaths.Count()} audio file(s) with resource ID \"{taggingSet.ResourceId}\"");

            string json;
            try
            {
                json = File.ReadAllText(taggingSet.JsonFilePath);
            }
            catch (Exception ex)
            {
                printer.Error($"Error reading JSON file \"{taggingSet.JsonFilePath}\": {ex.Message}.");
                continue;
            }

            YouTubeJson.Root? parsedJson;
            try
            {
                parsedJson = JsonSerializer.Deserialize<YouTubeJson.Root>(json);

                if (parsedJson is null)
                {
                    printer.Error($"The JSON from file \"{taggingSet.JsonFilePath}\" was unexpectedly null.");
                    continue;
                }
            }
            catch (JsonException ex)
            {
                printer.Error($"Error deserializing JSON from file \"{json}\": {ex.Message}");
                continue;
            }

            DeleteSourceFile(taggingSet, printer);

            foreach (var audioFilePath in taggingSet.AudioFilePaths)
            {
                try
                {
                    var audioFileName = Path.GetFileName(audioFilePath);
                    printer.Print($"Current audio file: \"{audioFileName}\"");

                    using var taggedFile = TagLib.File.Create(audioFilePath);
                    taggedFile.Tag.Title = DetectTitle(parsedJson, printer, parsedJson.title);
                    var maybeArtist = DetectArtist(parsedJson, printer);
                    if (maybeArtist is not null)
                    {
                        taggedFile.Tag.Performers = new[] { maybeArtist };
                    }
                    var maybeAlbum = DetectAlbum(parsedJson, printer);
                    if (maybeAlbum is not null)
                    {
                        taggedFile.Tag.Album = maybeAlbum;
                    }
                    taggedFile.Tag.Year = DetectReleaseYear(parsedJson, printer);
                    taggedFile.Tag.Comment = GenerateComment(parsedJson);
                    AddImage(taggedFile, taggingSet.ResourceId, workingDirectory, printer);
                    taggedFile.Save();
                    printer.Print($"Wrote tags to \"{audioFileName}\"");
                }
                catch (Exception ex)
                {
                    printer.Error($"Error tagging file: {ex.Message}");
                    continue;
                }
            }
        }

        return Result.Ok($"Tagging done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");

        static List<TaggingSet> CreateTagSets(IEnumerable<string> filePaths, string workingDirectory)
        {
            if (!filePaths.Any())
                return new List<TaggingSet>();

            return filePaths
                        .Select(f => _videoResourceIdRegex.Match(f))
                        .Where(m => m.Success)
                        .Select(m => m.Captures.OfType<Match>().First())
                        .GroupBy(m => m.Groups[1].Value,
                                 m => m.Groups[0].Value)
                        .Where(gr => gr.Count(f => f.EndsWith(_jsonFileExtension, StringComparison.OrdinalIgnoreCase)) == 1)
                        .Select(gr => {
                            return new TaggingSet(
                                gr.Key,
                                gr.Where(f => f.EndsWith(_audioFileExtension)), // # TODO: Support multiple formats.
                                gr.Where(f => f.EndsWith(_jsonFileExtension)).First());
                        }).ToList();
        }

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

        static string? DetectTitle(YouTubeJson.Root data, Printer printer, string? defaultName = null)
        {
            // TODO: Put this somewhere where it can be static.
            List<(string Regex, int Group, string Text, string Source)> parsePatterns = new()
            {
                (@"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D", 1, data.description, "description (Topic style)"),
            };

            foreach (var pattern in parsePatterns)
            {
                var regex = new Regex(pattern.Regex);
                var match = regex.Match(pattern.Text);

                if (match is not { Success: true })
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
                (@"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D", 2, data.description, "description (Topic style)"),
            };

            foreach (var pattern in parsePatterns)
            {
                var regex = new Regex(pattern.Regex);
                var match = regex.Match(pattern.Text);

                if (match is not { Success: true })
                    continue;

                printer.Print($"Writing artist \"{match.Groups[pattern.Group].Value}\" (matched via {pattern.Source})");
                return match.Groups[pattern.Group].Value.Trim();
            }

            return defaultName;
        }

        static string? DetectAlbum(YouTubeJson.Root data, Printer printer, string? defaultName = null)
        {
            // TODO: Put this somewhere where it can be static.
            List<(string Regex, int Group, string Text, string Source)> parsePatterns = new()
            {
                (@"(?<=[Aa]lbum: ).+", 0, data.description, "description"),
                (@"(.+) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\d{3})\D", 3, data.description, "description (Topic style)"),
                ("""(?<='s ['"]).+(?=['"] album)""", 0, data.description, "description"),
            };

            foreach (var pattern in parsePatterns)
            {
                var regex = new Regex(pattern.Regex);
                var match = regex.Match(pattern.Text);

                if (match is not { Success: true })
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
    /// Deletes the pre-split, source audio (if any) for split videos, each of which will have the same resource ID.
    /// </summary>
    /// <param name="taggingSet"></param>
    /// <param name="printer"></param>
    private static void DeleteSourceFile(TaggingSet taggingSet, Printer printer)
    {
        if (taggingSet.AudioFilePaths.Count() > 1)
        {
            var largestFileInfo =
                taggingSet.AudioFilePaths
                    .Select(fn => new FileInfo(fn))
                    .OrderByDescending(fi => fi.Length)
                    .First();

            try
            {
                File.Delete(largestFileInfo.FullName);
                taggingSet.AudioFilePaths.Remove(largestFileInfo.FullName);
                printer.Print($"Deleted pre-split source file \"{largestFileInfo.Name}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"Error deleting pre-split source file \"{largestFileInfo.Name}\": {ex.Message}");
            }
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
            printer.Print("Image written to file tags OK.");
        }
        catch (Exception ex)
        {
            printer.Error($"Error writing image to the audio file: {ex.Message}");
            return;
        }

        printer.Print("Image written to file OK.");
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

    /// <summary>
    /// Contains all of the data necessary for tagging files.
    /// </summary>
    public sealed class TaggingSet
    {
        public string ResourceId { get; init; }
        public List<string> AudioFilePaths { get; init; }
        public string JsonFilePath { get; init; }

        public TaggingSet(string resourceId, IEnumerable<string> audioFilePaths, string jsonFilePath)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                throw new ArgumentException("The resource ID must be provided");
            if (string.IsNullOrWhiteSpace(jsonFilePath))
                throw new ArgumentException("The JSON file path must be provided");

            ResourceId = resourceId.Trim();
            AudioFilePaths = audioFilePaths.ToList();
            JsonFilePath = jsonFilePath.Trim();
        }
    }
}
