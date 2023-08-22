using System;
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
            // printer.Warning($"Found {jsonFiles.Count()} JSON file(s).");
        }
        catch (Exception ex)
        {
            printer.Error($"Error reading {jsonFileSearchPattern} files: " + ex.Message);
            return;
        }

        var regex = new Regex(@".+\[([\w_\-]{11})\](?:.*)?\.(\w+)");
        var matches = jsonFiles
                        .Select(f => regex.Match(f))
                        .Where(m => m.Success)
                        .Select(m => m.Captures);
        var idsWithFileNames = matches.Select(m => m.OfType<Match>().First())
                                      .GroupBy(m => m.Groups[1].Value,
                                               m => m.Groups[0].Value);
        var invalid = idsWithFileNames.Where(g => g.Count() != 1);
        if (invalid.Any())
        {
            printer.Errors(
                invalid.Select(i => $"Too many JSON files for ID {i.Key}"),
                "JSON errors:"
            );
        }
        var valid = idsWithFileNames.Where(g => g.Count() == 1);
        // printer.Print($"Valid IDs: {string.Join(", ", valid.Select(i => i.Key))}");

        const string audioFileExtension = ".m4a";
        foreach (var idNamePair in valid)
        {
            var resourceId = idNamePair.Key;

            var jsonFileName = idNamePair.Single();
            string json;
            try
            {
                json = File.ReadAllText(jsonFileName);
            }
            catch (Exception ex)
            {
                printer.Error($"Error reading JSON file \"{jsonFileName}\": {ex.Message}.");
                continue;
            }

            YouTubeJson.Root? data;
            try
            {
                data = JsonSerializer.Deserialize<YouTubeJson.Root>(json);

                if (data is null)
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

            var audioFilesForThisID = Directory.GetFiles(workingDirectory, $"*{idNamePair.Key}*{audioFileExtension}");
            if (!audioFilesForThisID.Any())
            {
                printer.Error($"No {audioFileExtension} files for ID {idNamePair.Key} were found.");
                continue;
            }
            printer.Print($"Found {audioFilesForThisID.Count()} audio file(s) for resource ID {idNamePair.Key}");

            foreach (var audioFilePath in audioFilesForThisID)
            {
                var audioFileName = Path.GetFileName(audioFilePath);
                printer.Print($"Current audio file: \"{audioFileName}\"");
                using var taggedFile = TagLib.File.Create(audioFilePath);
                taggedFile.Tag.Title = data.title;
                // taggedFile.Tag. // TODO: Manually add 'original name' frame.
                taggedFile.Tag.Comment = GenerateComment(data);
                AddImage(taggedFile, resourceId, workingDirectory, printer);
                taggedFile.Save();
                printer.Print($"Wrote tags to \"{audioFileName}\"");
            }
        }

        printer.Print($"Tagging done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");

        static string GenerateComment(YouTubeJson.Root data)
        {
            StringBuilder sb = new();
            sb.AppendLine("SOURCE DATA:");
            sb.AppendLine($"• Downloaded: {DateTime.Now}");
            sb.AppendLine($"• Service: {data.extractor_key}");
            sb.AppendLine($"• Title: {data.fulltitle}");
            sb.AppendLine($"• URL: {data.webpage_url}");
            sb.AppendLine($"• Uploader: {data.uploader} ({data.uploader_url})");
            sb.AppendLine($"• Uploaded: {data.upload_date})");
            sb.AppendLine($"• Description: {data.description})");
            return sb.ToString();
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
            printer.Print("Image attached to tagged file.");
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
