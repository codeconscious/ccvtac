using System.IO;
using System.Text;
using System.Text.Json;

namespace CCVTAC.Console.PostProcessing;

public class PostProcessing
{
    public string WorkingDirectory { get; }
    public Printer Printer { get; }

    public PostProcessing(string workingDirectory, Printer printer)
    {
        WorkingDirectory = workingDirectory;
        Printer = printer;
    }

    internal void Run()
    {
        Printer.PrintLine("Starting post-processing...");

        Tagger.SetId3v2Version(
            version: Tagger.Id3v2Version.TwoPoint3,
            forceAsDefault: true);

        // TODO: Create an interface and iterate through them, calling `Run()`?
        ImageProcessor.Run(WorkingDirectory, Printer);
        Tagger.Run(WorkingDirectory, Printer);
        // AudioNormalizer.Run(WorkingDirectory, Printer); // TODO: `mp3gain`は無理なので、別のnormalize方法を要検討。
        Deleter.Run(WorkingDirectory, Printer);
        Mover.Run(WorkingDirectory, "/Users/jd/Downloads/NewMusic", Printer);

        Printer.PrintLine("Post-processing is done!");
    }

    private static class Deleter
    {
        public static void Run(string workingDirectory, Printer printer)
        {
            try
            {
                var dir = new DirectoryInfo(workingDirectory);
                List<string> deletableExtensions = new() { ".json", ".jpg" };

                foreach (var file in dir.EnumerateFiles("*")
                                        .Where(f => deletableExtensions.Contains(f.Extension)))
                {
                    file.Delete();
                    printer.PrintLine($"Deleted file \"{file.Name}\"");
                }
            }
            catch (Exception ex)
            {
                printer.Error($"Error deleting file: {ex.Message}");
            }
        }
    }

    private static class Mover
    {
        internal static void Run(string workingDirectory, string destinationDirectory, Printer printer)
        {
            uint movedCount = 0;
            var dir = new DirectoryInfo(workingDirectory);
            printer.PrintLine($"Moving audio files to \"{destinationDirectory}\"...");
            foreach (var file in dir.EnumerateFiles("*.m4a"))
            {
                try
                {
                    File.Move(file.FullName, $"{Path.Combine(destinationDirectory, file.Name)}");
                    printer.PrintLine($"Moved \"{file.Name}\"");
                    movedCount++;
                }
                catch (Exception ex)
                {
                    printer.Error($"Error moving file \"{file.Name}\": {ex.Message}");
                }
            }
            printer.PrintLine($"{movedCount} file(s) moved to \"{destinationDirectory}\"");
        }
    }

    private static class Tagger
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
                printer.PrintLine("Aborting image addition.");
                return;
            }

            try
            {
                var pics = new TagLib.IPicture[1];
                pics[0] = new TagLib.Picture(imageFile);
                taggedFile.Tag.Pictures = pics;
                printer.PrintLine("Image attached to tagged file.");
            }
            catch (Exception ex)
            {
                printer.Error($"Error attaching image to the tagged file: {ex.Message}");
                printer.PrintLine("Aborting image addition.");
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

        public static void SetId3v2Version(Id3v2Version version, bool forceAsDefault)
        {
            TagLib.Id3v2.Tag.DefaultVersion = (byte)version;
            TagLib.Id3v2.Tag.ForceDefaultVersion = forceAsDefault;
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

    private static class ImageProcessor
    {
        internal static void Run(string workingDirectory, Printer printer)
        {
            ExternalTools.ImageProcessor(workingDirectory, printer);
        }
    }

    private static class AudioNormalizer
    {
        internal static void Run(string workingDirectory, Printer printer)
        {
            ExternalTools.AudioNormalization(workingDirectory, printer);
        }
    }
}
