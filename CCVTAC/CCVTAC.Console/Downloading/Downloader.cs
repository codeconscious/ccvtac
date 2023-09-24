using CCVTAC.Console.Downloading.DownloadEntities;
using CCVTAC.Console.ExternalUtilities;
using CCVTAC.Console.Settings;

namespace CCVTAC.Console.Downloading;

internal static class Downloader
{
    internal static ExternalProgram ExternalProgram = new(
        "yt-dlp",
        "https://github.com/yt-dlp/yt-dlp/",
        "video download and audio extraction");

    internal static Result<string> Run(string url, UserSettings userSettings, Printer printer)
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
        printer.Print($"{downloadEntity.Type} URL '{url}' detected.");

        var args = GenerateDownloadArgs(
            userSettings,
            downloadEntity.Type,
            downloadEntity.FullResourceUrl);

        var downloadToolSettings = new ExternalUtilties.UtilitySettings(
            ExternalProgram,
            args,
            userSettings.WorkingDirectory!
        );

        var downloadResult = ExternalUtilties.Caller.Run(downloadToolSettings, printer);

        if (downloadResult.IsFailed)
        {
            downloadResult.Errors.ForEach(e => printer.Error(e.Message));
            printer.Warning("However, post-processing will still be attempted.");
        }

        return Result.Ok($"Downloading done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }

    /// <summary>
    /// Generate the argument string from the download tool.
    /// </summary>
    /// <returns>A string of arguments that can be passed directly to the download tool.</returns>
    private static string GenerateDownloadArgs(
        UserSettings     settings,
        DownloadType     downloadType,
        params string[]? additionalArgs)
    {
        var args = new List<string>() {
            $"--extract-audio -f {settings.AudioFormat}",
            "--write-thumbnail --convert-thumbnails jpg", // For album art
            "--write-info-json", // For parsing metadata
        };

        if (settings.SplitChapters)
        {
            args.Add("--split-chapters");
        }

        if (downloadType is not DownloadType.Video)
        {
            args.Add($"--sleep-interval {settings.SleepBetweenDownloadsSeconds}");
        }

        return string.Join(" ", args.Concat(additionalArgs ?? Enumerable.Empty<string>()));
    }
}
