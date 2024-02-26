using CCVTAC.Console.ExternalUtilities;
using CCVTAC.Console.Settings;
using MediaType = CCVTAC.FSharp.Downloading.MediaType; // Types from the F# library

namespace CCVTAC.Console.Downloading;

internal static class Downloader
{
    internal static ExternalProgram ExternalTool = new(
        "yt-dlp",
        "https://github.com/yt-dlp/yt-dlp/",
        "YouTube media and metadata downloads, plus audio extraction"
    );

    /// <summary>
    /// All known error codes returned by yt-dlp with their meanings.
    /// </summary>
    /// <remarks>Source: https://github.com/yt-dlp/yt-dlp/issues/4262#issuecomment-1173133105</remarks>
    internal static Dictionary<int, string> ExitCodes = new()
    {
        { 0, "Success" },
        { 1, "Unspecified error" },
        { 2, "Error in provided options" },
        { 100, "yt-dlp must restart for update to complete" },
        { 101, "Download cancelled by --max-downloads, etc." },
    };

    internal static Result<string> Run(string url, UserSettings userSettings, Printer printer)
    {
        Watch watch = new();

        var mediaTypeOrError = FSharp.Downloading.mediaTypeWithIds(url);
        if (mediaTypeOrError.IsError)
        {
            return Result.Fail("Unable to determine the type of URL.");
        }

        var mediaType = mediaTypeOrError.ResultValue;
        printer.Print($"{mediaType.GetType().Name} URL '{url}' detected.");

        var urls = FSharp.Downloading.downloadUrls(mediaType);

        string args = GenerateDownloadArgs(userSettings, mediaType, urls[0]);
        var downloadToolSettings = new UtilitySettings(ExternalTool, args, userSettings.WorkingDirectory!, ExitCodes);
        var downloadResult = Runner.Run(downloadToolSettings, printer);

        if (downloadResult.IsFailed)
        {
            downloadResult.Errors.ForEach(e => printer.Error(e.Message));
            printer.Warning("However, post-processing will still be attempted."); // TODO: これで良い？
            // TODO: Seems we can return here.
        }
        // Do the supplementary download, if any such data was passed in.
        else if (urls.Length > 1) // Meaning there's a supplementary URL for downloading playlist metadata.
        {
            string supplementaryArgs = GenerateDownloadArgs(
                userSettings,
                null, // Indicates a metadata-only supplementary download. Will improve later.
                urls[1]
            );

            UtilitySettings supplementaryDownloadSettings = new(
                ExternalTool,
                supplementaryArgs,
                userSettings.WorkingDirectory!,
                ExitCodes);

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
                                               MediaType? mediaType,
                                               params string[]? additionalArgs)
    {
        const string writeJson = "--write-info-json";
        const string trim = "--trim-filenames 250";

        HashSet<string> args = mediaType switch
        {
            null => [ $"--flat-playlist {writeJson} {trim}" ], // Metadata only
            _ => [
                     $"--extract-audio -f {settings.AudioFormat}",
                     "--write-thumbnail --convert-thumbnails jpg", // For album art
                     writeJson, // For parsing metadata
                     trim,
                     "--retries 3", // Default is 10, which seems more than necessary
                 ]
        };

        // `--verbose` is a yt-dlp option too, but maybe that's too much data.
        // It might be worth incorporating it in the future as a third option.
        args.Add(settings.VerboseOutput ? string.Empty : "--quiet --progress");

        if (mediaType is not null)
        {
            if (settings.SplitChapters)
            {
                args.Add("--split-chapters");
            }

            // List<Type> singleDownloadTypes = [typeof(FMediaType.Video), typeof(FMediaType.PlaylistVideo)];
            // if (!singleDownloadTypes.Contains(mediaType.GetType()))
            if (!mediaType.IsVideo && !mediaType.IsPlaylistVideo)
            {
                args.Add($"--sleep-interval {settings.SleepSecondsBetweenDownloads}");
            }

            // The numbering of regular playlists should be reversed because the newest items are
            // always placed at the top of the list at position #1. Instead, the oldest items
            // (at the end of the list) should begin at #1.
            // if (mediaType.GetType() == typeof(FMediaType.StandardPlaylist))
            if (mediaType.IsStandardPlaylist)
            {
                // The digits followed by `B` induce trimming to the specified number of bytes.
                // Use `s` instead of `B` to trim to a specified number of characters.
                // Reference: https://github.com/yt-dlp/yt-dlp/issues/1136#issuecomment-1114252397
                // Also, it's possible this trimming should be applied to `ReleasePlaylist`s too.
                args.Add("""-o "%(playlist).80B = %(playlist_autonumber)s - %(title).150B [%(id)s].%(ext)s" --playlist-reverse""");
            }
        }

        return string.Join(" ", args.Concat(additionalArgs ?? []));
    }
}
