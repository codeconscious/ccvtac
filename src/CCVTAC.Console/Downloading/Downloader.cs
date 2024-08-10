using CCVTAC.Console.ExternalTools;
using MediaTypeWithUrls = CCVTAC.FSharp.Downloading.MediaType;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.Downloading;

internal static class Downloader
{
    private record Urls(string Primary, string? Supplementary);

    internal static ExternalTool ExternalTool = new(
        "yt-dlp",
        "https://github.com/yt-dlp/yt-dlp/",
        "YouTube downloads and audio extraction"
    );

    internal static Result<MediaTypeWithUrls> WrapUrlInMediaType(string url)
    {
        var result = FSharp.Downloading.MediaTypeWithIds(url);

        return result.IsOk
            ? Result.Ok(result.ResultValue)
            : Result.Fail(result.ErrorValue);
    }

    /// <summary>
    /// Completes the actual download process.
    /// </summary>
    /// <returns>A `Result` that, if successful, contains the name of the successfully downloaded format.</returns>
    internal static Result<string?> Run(MediaTypeWithUrls mediaType, UserSettings settings, Printer printer)
    {
        Watch watch = new();

        if (!mediaType.IsVideo && !mediaType.IsPlaylistVideo)
        {
            printer.Info("Please wait for the multiple videos to be downloaded...");
        }

        var rawUrls = FSharp.Downloading.ExtractDownloadUrls(mediaType);
        var urls = new Urls(rawUrls[0], rawUrls.Length == 2 ? rawUrls[1] : null);

        Result downloadResult = new();
        string? successfulFormat = null;

        foreach (string format in settings.AudioFormats)
        {
            string args = GenerateDownloadArgs(format, settings, mediaType, urls.Primary);
            var downloadSettings = new ToolSettings(ExternalTool, args, settings.WorkingDirectory!);

            downloadResult = Runner.Run(downloadSettings, printer);

            if (downloadResult.IsSuccess)
            {
                successfulFormat = format;
                break;
            }

            printer.Debug($"Failure downloading \"{format}\" format.");
        }

        var errors = downloadResult.Errors.Select(e => e.Message).ToList();

        if (downloadResult.IsFailed)
        {
            downloadResult.Errors.ForEach(e => printer.Error(e.Message));
            printer.Info("Post-processing will still be attempted."); // For any partial downloads
        }
        else if (urls.Supplementary is not null)
        {
            string supplementaryArgs = GenerateDownloadArgs(null, settings, null, urls.Supplementary);

            var supplementaryDownloadSettings =
                new ToolSettings(
                    ExternalTool,
                    supplementaryArgs,
                    settings.WorkingDirectory!);

            var supplementaryDownloadResult = Runner.Run(supplementaryDownloadSettings, printer);

            if (supplementaryDownloadResult.IsSuccess)
            {
                printer.Info("Supplementary download completed OK.");
            }
            else
            {
                printer.Error("Supplementary download failed.");
                errors.AddRange(supplementaryDownloadResult.Errors.Select(e => e.Message));
            }
        }

        return errors.Count > 0
            ? Result.Fail(string.Join(" / ", errors))
            : Result.Ok(successfulFormat);
    }

    /// <summary>
    /// Generate the entire argument string for the download tool.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="mediaType">A `MediaType` or null (which indicates a metadata-only supplementary download).</param>
    /// <param name="additionalArgs"></param>
    /// <returns>A string of arguments that can be passed directly to the download tool.</returns>
    private static string GenerateDownloadArgs(
        string? audioFormat,
        UserSettings settings,
        MediaTypeWithUrls? mediaType,
        params string[]? additionalArgs)
    {
        const string writeJson = "--write-info-json";
        const string trimFileNames = "--trim-filenames 250";

        // yt-dlp warning: "-f best" selects the best pre-merged format which is often not the best option.
        // To let yt-dlp download and merge the best available formats, simply do not pass any format selection."
        var formatArg = !audioFormat.HasText() || audioFormat == "best"
            ? string.Empty
            : $"-f {audioFormat}";

        HashSet<string> args = mediaType switch
        {
            // Metadata-only download
            null => [ $"--flat-playlist {writeJson} {trimFileNames}" ],

            // Video(s) with their metadata
            _ => [
                    //  $"--extract-audio -f m4a",
                     $"--extract-audio",
                     "--write-thumbnail --convert-thumbnails jpg", // For album art
                     writeJson, // Contains metadata
                     trimFileNames,
                     "--retries 2", // Default is 10, which seems like overall
                 ]
        };

        // yt-dlp has a `--verbose` option too, but that's too much data.
        // It might be worth incorporating it in the future as a third option.
        args.Add(settings.QuietMode ? "--quiet --no-warnings" : string.Empty);

        if (mediaType is not null)
        {
            if (settings.SplitChapters)
            {
                args.Add("--split-chapters");
            }

            if (!mediaType.IsVideo && !mediaType.IsPlaylistVideo)
            {
                args.Add($"--sleep-interval {settings.SleepSecondsBetweenDownloads}");
            }

            // The numbering of regular playlists should be reversed because the newest items are
            // always placed at the top of the list at position #1. Instead, the oldest items
            // (at the end of the list) should begin at #1.
            if (mediaType.IsStandardPlaylist)
            {
                // The digits followed by `B` induce trimming to the specified number of bytes.
                // Use `s` instead of `B` to trim to a specified number of characters.
                // Reference: https://github.com/yt-dlp/yt-dlp/issues/1136#issuecomment-1114252397
                // Also, it's possible this trimming should be applied to `ReleasePlaylist`s too.
                args.Add("""-o "%(uploader).80B - %(playlist).80B - %(playlist_autonumber)s - %(title).150B [%(id)s].%(ext)s" --playlist-reverse""");
            }
        }

        return string.Join(" ", args.Concat(additionalArgs ?? []));
    }
}
