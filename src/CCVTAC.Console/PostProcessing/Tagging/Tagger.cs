using System.IO;
using System.Text.Json;
using TaggedFile = TagLib.File;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.PostProcessing.Tagging;

internal static class Tagger
{
    internal static Result<string> Run(UserSettings settings,
                                       IEnumerable<TaggingSet> taggingSets,
                                       CollectionMetadata? collectionJson,
                                       Printer printer)
    {
        if (settings.VerboseOutput)
            printer.Print("Adding file tags...");

        Watch watch = new();

        foreach (TaggingSet taggingSet in taggingSets)
        {
            ProcessSingleTaggingSet(settings, taggingSet, collectionJson, printer);
        }

        return Result.Ok($"Tagging done in {watch.ElapsedFriendly}.");
    }

    private static void ProcessSingleTaggingSet(
        UserSettings settings,
        TaggingSet taggingSet,
        CollectionMetadata? collectionJson,
        Printer printer)
    {
        if (settings.VerboseOutput)
            printer.Print($"{taggingSet.AudioFilePaths.Count} audio file(s) with resource ID \"{taggingSet.ResourceId}\"");

        var parsedJsonResult = ParseVideoJson(taggingSet);
        if (parsedJsonResult.IsFailed)
        {
            printer.Errors(
                $"Error deserializing video metadata from \"{taggingSet.JsonFilePath}\":",
                parsedJsonResult);
            return;
        }

        TaggingSet finalTaggingSet = DeleteSourceFile(taggingSet, printer, settings.VerboseOutput);

        foreach (string audioFilePath in finalTaggingSet.AudioFilePaths)
        {
            TagSingleFile(
                settings,
                parsedJsonResult.Value,
                audioFilePath,
                taggingSet.ImageFilePath,
                collectionJson,
                printer
            );
        }
    }

    static void TagSingleFile(UserSettings settings,
                              VideoMetadata videoData,
                              string audioFilePath,
                              string imageFilePath,
                              CollectionMetadata? collectionData,
                              Printer printer)
    {
        try
        {
            string audioFileName = Path.GetFileName(audioFilePath);

            if (settings.VerboseOutput)
                printer.Print($"Current audio file: \"{audioFileName}\"");

            using TaggedFile taggedFile = TaggedFile.Create(audioFilePath);
            TagDetector tagDetector = new();

            if (videoData.Track is string metadataTitle)
            {
                if (settings.VerboseOutput)
                    printer.Print($"• Using metadata title \"{metadataTitle}\"");

                taggedFile.Tag.Title = metadataTitle;
            }
            else
            {
                if (settings.VerboseOutput)
                    printer.Print($"• Found title \"{taggedFile.Tag.Title}\"");

                taggedFile.Tag.Title = tagDetector.DetectTitle(videoData, videoData.Title);
            }

            if (videoData.Artist is string metadataArtists)
            {
                var firstArtist = metadataArtists.Split(", ").First();
                var diffSummary = firstArtist == metadataArtists
                    ? string.Empty
                    : $" (extracted from \"{metadataArtists}\")";
                taggedFile.Tag.Performers = [firstArtist];

                if (settings.VerboseOutput)
                    printer.Print($"• Using metadata artist \"{firstArtist}\"{diffSummary}");
            }
            else if (tagDetector.DetectArtist(videoData) is string artist)
            {
                if (settings.VerboseOutput)
                    printer.Print($"• Found artist \"{artist}\"");

                taggedFile.Tag.Performers = [artist];
            }

            if (videoData.Album is string metadataAlbum)
            {
                if (settings.VerboseOutput)
                    printer.Print($"• Using metadata album \"{metadataAlbum}\"");

                taggedFile.Tag.Album = metadataAlbum;
            }
            else if (tagDetector.DetectAlbum(videoData, collectionData?.Title) is string album)
            {
                if (settings.VerboseOutput)
                    printer.Print($"• Found album \"{album}\"");

                taggedFile.Tag.Album = album;
            }

            if (tagDetector.DetectComposers(videoData) is string composers)
            {
                if (settings.VerboseOutput)
                    printer.Print($"• Found composer(s) \"{composers}\"");

                taggedFile.Tag.Composers = [composers];
            }

            if (videoData.PlaylistIndex is uint trackNo)
            {
                if (settings.VerboseOutput)
                    printer.Print($"• Using playlist index of {trackNo} for track number");

                taggedFile.Tag.Track = trackNo;
            }

            if (videoData.ReleaseYear is uint releaseYear)
            {
                if (settings.VerboseOutput)
                    printer.Print($"• Using metadata release year \"{releaseYear}\"");

                taggedFile.Tag.Year = releaseYear;
            }
            else
            {
                ushort? maybeDefaultYear = GetAppropriateReleaseDateIfAny(settings, videoData);

                if (tagDetector.DetectReleaseYear(videoData, maybeDefaultYear) is ushort year)
                {
                    if (settings.VerboseOutput)
                        printer.Print($"• Found year \"{year}\"");

                    taggedFile.Tag.Year = year;
                }
            }

            taggedFile.Tag.Comment = videoData.GenerateComment(collectionData);

            WriteImage(taggedFile, imageFilePath, printer, settings.VerboseOutput);
            taggedFile.Save();
            if (settings.VerboseOutput)
                printer.Print($"Wrote tags to \"{audioFileName}\".");
        }
        catch (Exception ex)
        {
            printer.Error($"Error tagging file: {ex.Message}");
        }

        /// <summary>
        /// If the supplied video uploader is specified in the settings, returns the video's upload year.
        /// Otherwise, returns null.
        /// </summary>
        static ushort? GetAppropriateReleaseDateIfAny(UserSettings settings, VideoMetadata videoData)
        {
            if (settings.IgnoreUploadYearUploaders?.Contains(videoData.Uploader,
                                                             StringComparer.OrdinalIgnoreCase) == true)
            {
                return null;
            }

            return ushort.TryParse(videoData.UploadDate[0..4], out ushort parsedYear)
                ? parsedYear
                : null;
        }
    }

    static Result<VideoMetadata> ParseVideoJson(TaggingSet taggingSet)
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
            return Result.Fail($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Deletes the pre-split source audio for split videos (if any), each of which will have the same resource ID.
    /// Returns a modified TaggingSet with the deleted files removed.
    /// </summary>
    /// <param name="taggingSet"></param>
    /// <param name="printer"></param>
    private static TaggingSet DeleteSourceFile(TaggingSet taggingSet, Printer printer, bool verbose)
    {
        // If there is only one file, then there are no child files, so no action is necessary.
        if (taggingSet.AudioFilePaths.Count <= 1)
        {
            return taggingSet;
        }

        // If a video is split, it must have at least 2 chapters, so the minimum possible
        // audio file count is 3. (Returning the taggingSet as-is might be viable, though.)
        if (taggingSet.AudioFilePaths.Count == 2)
        {
            throw new InvalidOperationException(
                $"Two audio files were found for media ID \"{taggingSet.ResourceId}\", but this should be impossible, so cannot continue.");
        }

        // The largest audio file must be the source file.
        FileInfo largestFileInfo =
            taggingSet.AudioFilePaths
                .Select(fileName => new FileInfo(fileName))
                .OrderByDescending(fi => fi.Length)
                .First();

        try
        {
            File.Delete(largestFileInfo.FullName);

            if (verbose)
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
    /// <remarks>Heavily inspired by https://stackoverflow.com/a/61264720/11767771.</remarks>
    private static void WriteImage(TaggedFile taggedFile, string imageFilePath, Printer printer, bool verbose)
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

            if (verbose)
                printer.Print("Image written to file tags OK.");
        }
        catch (Exception ex)
        {
            printer.Error($"Error writing image to the audio file: {ex.Message}");
            return;
        }
    }
}
