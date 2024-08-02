namespace CCVTAC.Console;

public static class Help
{
    internal static void Print(Printer printer)
    {
        string helpText = """"
        CCVTAC (CodeConscious Video-to-Audio Converter) is a small .NET-powered CLI
        tool that acts as a wrapper around yt-dlp (https://github.com/yt-dlp/yt-dlp)
        to enable easier downloads of M4A audio from YouTube videos, playlist, and
        channels, plus do some automatic post-processing (tagging, renaming, and
        moving) as well.

        While I maintain it primarily for my own use, feel free to use it yourself.
        No warranties or guarantees provided!

        PREREQUISITES:

        • .NET 8 runtime (https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
        • yt-dlp https://github.com/yt-dlp/yt-dlp
        • [ffmpeg](https://ffmpeg.org/) (for yt-dlp artwork extraction)
        • Optional: mogrify https://imagemagick.org/script/mogrify.php
                    (for auto-trimming album art)

        RUNNING IT

        Settings:

        A valid settings file is mandatory to use this application.

        The application will look for a file named `settings.json` in its directory.
        However, you can manually specify an existing file path using the `-s`
        option, such as `dotnet run -- -s <PATH_TO_YOUR_FILE>`.

        If your `settings.json` file does not exist, a default file will be created in the
        application directory with default settings. At minimum, you will need to
        enter (1) an existing directory for temporary working files, (2) an existing
        directory to which the final audio files should be moved, and (3) a path to
        your history file. The other settings have sensible defaults.

        I added the `sleepSecondsBetweenDownloads` and `sleepSecondsBetweenBatches`
        settings to help reduce concentrated loads on YouTube servers. Please avoid
        lowering these values too much and slamming their servers with enormous,
        long-running downloads (even if you feel their servers can take it).

        See the README file on the GitHub repo for more about settings.

        Using the application:

        Once your settings are ready, run the application with `dotnet run`.
        Optionally, pass `-h` or `--help` for instructions (e.g., `dotnet run -- --help`).

        When the application is running, simply enter at least one YouTube media URL
        (video, playlist, or channel) at the prompt and press the Enter key.
        You can optionally omit spaces between the URLs.

        You can also enter the following commands:
        - `\quit` or `\q` to quit
        - `\history` to see the last few URLs you entered
        - `\split` to toggle chapter splitting for the current session only
        - `\images` to toggle image embedding for the current session only
        - `\verbose` to toggle log verbosity for the current session only

        Upgrading yt-dlp:

        Periodically ensure you are running the latest version of yt-dlp, especially
        if you start experiencing download errors. See the yt-dlp GitHub page
        https://github.com/yt-dlp/yt-dlp#update for more. (Likely commands are `sudo
        yt-dlp -U` or `pip install --upgrade yt-dlp`.)

        Reporting issues:

        If you run into any issues, feel free to create an issue on GitHub with as much
        information as possible (e.g., entered URLs, system information, yt-dlp version).
        """";

        printer.Print(helpText, processMarkup: false);
    }
}
