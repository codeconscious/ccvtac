using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CCVTAC.Console.PostProcessing;

internal static class Tagger
{
    internal static void Run(string workingDirectory, Printer printer)
    {
        string jsonFile;
        try
        {
            jsonFile = Directory.GetFiles(workingDirectory, "*.json").Single();
        }
        catch (Exception ex)
        {
            printer.Error("Error finding JSON file: " + ex.Message);
            return;
        }

        string json;
        try
        {
            json = File.ReadAllText(jsonFile);
        }
        catch (Exception ex)
        {
            printer.Error($"Error reading JSON file \"{jsonFile}\": {ex.Message}");
            return;
        }

        YouTubeJson.Root? data;
        try
        {
            data = JsonSerializer.Deserialize<YouTubeJson.Root>(json);

            if (data is null)
            {
                printer.Error($"Invalid JSON from file \"{jsonFile}\" was unexpectedly null!");
                return;
            }
        }
        catch (JsonException ex)
        {
            printer.Error($"Error deserializing JSON from file \"{jsonFile}\": {ex.Message}");
            return;
        }

        string audioFile;
        try
        {
            audioFile = Directory.GetFiles(workingDirectory, "*.m4a").Single();
        }
        catch (InvalidOperationException ex)
        {
            printer.Error($"Error reading audio files in \"{workingDirectory}\": {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            printer.Error($"Error getting file from \"{workingDirectory}\": {ex.Message}");
            return;
        }

        using var taggedFile = TagLib.File.Create(audioFile);
        taggedFile.Tag.Title = data.title;
        taggedFile.Tag.Comment = GenerateComment(data);
        AddImage(taggedFile, workingDirectory, printer);
        taggedFile.Save();
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="taggedFile"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="printer"></param> <summary>
    /// <remarks>Heavily inspired by https://stackoverflow.com/a/61264720/11767771.</remarks>
    private static void AddImage(TagLib.File taggedFile, string workingDirectory, Printer printer)
    {
        string imageFile;
        try
        {
            imageFile = Directory.GetFiles(workingDirectory, "*.jpg").Single();
        }
        catch (Exception ex)
        {
            printer.Error($"Error finding a single image in \"{workingDirectory}\": {ex.Message}");
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
            printer.Error($"Error attaching image to the tagged file: {ex.Message}");
            printer.Print("Aborting image addition.");
            return;
        }
    }

    private static string GenerateComment(YouTubeJson.Root data)
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
