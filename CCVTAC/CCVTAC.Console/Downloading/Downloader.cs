using CCVTAC.Console.Downloading.Entities;
using CCVTAC.Console.ExternalUtilities;
using CCVTAC.Console.Settings;

namespace CCVTAC.Console.Downloading;

internal static class Downloader
{
    internal static ExternalProgram ExternalProgram = new(
        "yt-dlp",
        "https://github.com/yt-dlp/yt-dlp/",
        "YouTube media and metadata downloads, plus audio extraction"
    );

    /// <summary>
    /// All known error codes returned by yt-dlp with their meanings.
    /// </summary>
    /// <remarks>Source: https://github.com/yt-dlp/yt-dlp/issues/4262#issuecomment-1173133105</remarks>
    internal static Dictionary<int, string> DownloaderExitCodes = new()
    {
        { 0, "Success" },
        { 1, "Unspecific error" },
        { 2, "Error in provided options" },
        { 100, "yt-dlp must restart for update to complete" },
        { 101, "Download cancelled by --max-downloads, etc." },
    };

    internal static Result<string> Run(string url, UserSettings userSettings, Printer printer)
    {
        Watch watch = new();

        var downloadEntityResult = DownloadEntityFactory.Create(url);
        if (downloadEntityResult.IsFailed)
        {
            return Result.Fail(downloadEntityResult.Errors?.First().Message
                               ?? "An unknown error occurred parsing the resource type.");
        }
        IDownloadEntity downloadEntity = downloadEntityResult.Value;
        printer.Print($"{downloadEntity.VideoDownloadType} URL '{url}' detected.");

        string args = GenerateDownloadArgs(
            userSettings,
            downloadEntity.DownloadType,
            downloadEntity.VideoDownloadType,
            downloadEntity.PrimaryResource.FullResourceUrl);

        UtilitySettings downloadToolSettings = new(
            ExternalProgram,
            args,
            userSettings.WorkingDirectory!,
            DownloaderExitCodes
        );

        Result downloadResult = Runner.Run(downloadToolSettings, printer);

        if (downloadResult.IsFailed)
        {
            downloadResult.Errors.ForEach(e => printer.Error(e.Message));
            printer.Warning("However, post-processing will still be attempted."); // TODO: これで良い？
        }

        // Do the supplementary download, if any such data was passed in.
        if (downloadResult.IsSuccess &&
            downloadEntity.SupplementaryResource is ResourceUrlSet supplementary)
        {
            // TODO: Extract this content and the near-duplicate one above into a new method.
            string supplementaryArgs = GenerateDownloadArgs(
                userSettings,
                DownloadType.Metadata,
                null,
                supplementary.FullResourceUrl
            );

            UtilitySettings supplementaryDownloadSettings = new(
                ExternalProgram,
                supplementaryArgs,
                userSettings.WorkingDirectory!,
                DownloaderExitCodes            );

            Result<int> supplementaryDownloadResult = Runner.Run(supplementaryDownloadSettings, printer);

            if (supplementaryDownloadResult.IsSuccess)
            {
                printer.Print("Supplementary download completed OK.");
            }
            else
            {
                printer.Error("Supplementary download failed.");
                supplementaryDownloadResult.Errors.ForEach(e => printer.Error(e.Message));
            }
        }

        return Result.Ok($"Downloading done in {watch.ElapsedFriendly}.");
    }

    /// <summary>
    /// Generate the argument string from the download tool.
    /// </summary>
    /// <returns>A string of arguments that can be passed directly to the download tool.</returns>
    private static string GenerateDownloadArgs(UserSettings settings,
                                               DownloadType downloadType,
                                               MediaDownloadType? videoDownloadType,
                                               params string[]? additionalArgs)
    {
        HashSet<string> args = downloadType switch
        {
            DownloadType.Metadata => [ "--flat-playlist --write-info-json" ],
            _ => [
                     $"--extract-audio -f {settings.AudioFormat}",
                     "--write-thumbnail --convert-thumbnails jpg", // For album art
                     "--write-info-json", // For parsing metadata
                     "--retries 3" // Default is 10, more than necessary
                 ]
        };

        if (settings.SplitChapters && downloadType == DownloadType.Media)
        {
            args.Add("--split-chapters");
        }

        // `--verbose` is a yt-dlp option too, but maybe that's too much data.
        // It might be worth incorporating it in the future as a third option.
        args.Add(settings.VerboseOutput ? string.Empty : "--quiet --progress");

        if (downloadType is DownloadType.Media &&
            videoDownloadType is not MediaDownloadType.Video &&
            videoDownloadType is not MediaDownloadType.VideoOnPlaylist)
        {
            args.Add($"--sleep-interval {settings.SleepSecondsBetweenDownloads}");
        }

        // The numbering of regular playlists should be reversed because the newest items are
        // always placed at the top of the list at position #1. Instead, the oldest items
        // (at the end of the list) should begin at #1.
        if (downloadType is DownloadType.Media &&
            videoDownloadType is MediaDownloadType.Playlist)
        {
            args.Add("""-o "%(playlist)s = %(playlist_autonumber)s - %(title)s [%(id)s].%(ext)s" --playlist-reverse""");
        }

        return string.Join(" ", args.Concat(additionalArgs ?? []));
    }
}
