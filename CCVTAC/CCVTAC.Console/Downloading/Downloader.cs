using System.IO;
using CCVTAC.Console.Downloading.DownloadEntities;
using CCVTAC.Console.Settings;

namespace CCVTAC.Console.Downloading;

public static class Downloader
{
    internal static Result<string> Run(string url, UserSettings settings, Printer printer)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        var downloadEntityResult = DownloadEntityFactory.Create(url);
        if (downloadEntityResult.IsFailed)
        {
            return Result.Fail(downloadEntityResult.Errors?.First().Message
                               ?? "An unknown error occurred parsing the resource type.");
        }
        var downloadEntity = downloadEntityResult.Value;
        printer.Print($"Processing {downloadEntity.GetType().Name.ToLowerInvariant()} URL...");

        var downloadToolSettings = new ExternalUtilties.ExternalToolSettings(
            "video download and audio extraction",
            "yt-dlp",
            GenerateDownloadArgs(settings, url),
            settings.WorkingDirectory!,
            printer
        );
        var downloadResult = ExternalUtilties.Caller.Run(downloadToolSettings);
        if (downloadResult.IsFailed)
        {
            downloadResult.Errors.ForEach(e => printer.Error(e.Message));
            printer.Warning("However, post-processing will still be attempted.");
        }

        return Result.Ok($"Downloading done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }

    private static string GenerateDownloadArgs(UserSettings settings, params string[]? additionalArgs)
    {
        var args = new List<string>() {
            $"--extract-audio -f {settings.AudioFormat}",
            "--write-thumbnail --convert-thumbnails jpg", // For writing album art
            "--write-info-json", // For parsing and writing metadata
        };

        if (settings.SplitChapters)
            args.Add("--split-chapters");

        return string.Join(" ", args.Concat(additionalArgs ?? Enumerable.Empty<string>()));
    }
}
