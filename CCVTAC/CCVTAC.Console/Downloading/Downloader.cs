using System.Diagnostics;
using CCVTAC.Console.Downloading.DownloadEntities;
using CCVTAC.Console.ExternalUtilities;
using CCVTAC.Console.Settings;

namespace CCVTAC.Console.Downloading;

internal static class Downloader
{
    internal static ExternalProgram ExternalProgram = new(
        "yt-dlp",
        "https://github.com/yt-dlp/yt-dlp/",
        "video download and audio extraction"
    );

    internal static Result<string> Run(string url,
                                       UserSettings userSettings,
                                       Printer printer)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        var downloadEntityResult = DownloadEntityFactory.Create(url);
        if (downloadEntityResult.IsFailed)
        {
            return Result.Fail(downloadEntityResult.Errors?.First().Message
                               ?? "An unknown error occurred parsing the resource type.");
        }
        IDownloadEntity downloadEntity = downloadEntityResult.Value;
        printer.Print($"{downloadEntity.Type} URL '{url}' detected.");

        string args = GenerateDownloadArgs(
            userSettings,
            downloadEntity.Type,
            downloadEntity.PrimaryResource.FullResourceUrl);

        UtilitySettings downloadToolSettings = new(
            ExternalProgram,
            args,
            userSettings.WorkingDirectory!
        );

        var downloadResult = Runner.Run(downloadToolSettings, printer);

        if (downloadResult.IsFailed)
        {
            downloadResult.Errors.ForEach(e => printer.Error(e.Message));
            printer.Warning("However, post-processing will still be attempted."); // TODO: これで良い？
        }

        if (downloadResult.IsSuccess &&
            downloadEntity.SupplementaryResource is ResourceUrlSet supplementary)
        {
            // TODO: Extract this content and the near-duplicate one above into a new method.
            string supplementaryArgs = GenerateDownloadArgs(
                userSettings,
                downloadEntity.Type,
                supplementary.FullResourceUrl,
                // Download the list of video IDs in the associated playlist:
                "--flat-playlist --print id --skip-download --write-info-json"
            );

            UtilitySettings supplementaryDownloadToolSettings = new(
                ExternalProgram,
                supplementaryArgs,
                userSettings.WorkingDirectory!,
                true
            );

            var supplementaryDownloadResult = Runner.Run(supplementaryDownloadToolSettings, printer);

            if (supplementaryDownloadResult.IsSuccess)
            {
                printer.Print("Supplementary download completed OK.");
            }
            else
            {
                supplementaryDownloadResult.Errors.ForEach(e => printer.Error(e.Message));
                printer.Warning("However, post-processing will still be attempted."); // TODO: これで良い？
            }
        }

        return Result.Ok($"Downloading done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }

    /// <summary>
    /// Generate the argument string from the download tool.
    /// </summary>
    /// <returns>A string of arguments that can be passed directly to the download tool.</returns>
    private static string GenerateDownloadArgs(UserSettings settings,
                                               DownloadType downloadType,
                                               params string[]? additionalArgs)
    {
        HashSet<string> args =
        [
            $"--extract-audio -f {settings.AudioFormat}",
            "--write-thumbnail --convert-thumbnails jpg", // For album art
            "--write-info-json", // For parsing metadata
        ];

        if (settings.SplitChapters)
        {
            args.Add("--split-chapters");
        }

        // TODO: Move this logic to the individual types, via the interface.
        if (downloadType is not DownloadType.Video &&
            downloadType is not DownloadType.VideoOnPlaylist)
        {
            args.Add($"--sleep-interval {settings.SleepSecondsBetweenDownloads}");
        }

        return string.Join(" ", args.Concat(additionalArgs ?? Enumerable.Empty<string>()));
    }
}
