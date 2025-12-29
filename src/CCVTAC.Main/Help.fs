namespace CCVTAC.Main

module Help =

    let helpText = """
        CCVTAC (CodeConscious Video-to-Audio Converter) is a small .NET-powered CLI
        tool that acts as a wrapper around yt-dlp (https://github.com/yt-dlp/yt-dlp)
        to enable easier downloads of audio from YouTube videos, playlists, and
        channels, plus do some automatic post-processing (tagging, renaming, and
        moving) too.

        While I maintain it primarily for my own use, feel free to use it yourself.
        No warranties or guarantees are provided.

        FEATURES

        - Converts YouTube videos, playlists, and channels to local audio files (via yt-dlp)
        - Writes ID3 tags to files where possible using available or regex-detected metadata
        - Adds video metadata (channel name and URL, video URL, etc.) to files' Comment tags
        - Auto-renames files via custom regex patterns (to remove media IDs, etc.)
        - Optionally writes video thumbnails to files as artwork (if mogrify is installed)
        - Customized behavior via a user settings file -- e.g., chapter splitting, image embedding, directories
        - Saves entered URLs to a local history file

        PREREQUISITES

        • .NET 10 runtime (https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
        • yt-dlp (https://github.com/yt-dlp/yt-dlp)
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

        I added the `sleepSecondsBetweenDownloads` and `sleepSecondsBetweenURLs`
        settings to help reduce concentrated loads on YouTube servers. Please avoid
        lowering these values too much and slamming their servers with enormous,
        long-running downloads (even if you feel their servers can take it). Such behavior
        might get you rate-limited by YouTube.

        See the README file on the GitHub repo for more about settings.

        Using the application:

        Once your settings are ready, run the application with `dotnet run`.
        Alternatively, pass `-h` or `--help` for instructions (e.g.,
        `dotnet run -- --help`).

        When the application is running, enter at least one YouTube media URL (video,
        playlist, or channel) at the prompt and press Enter. No spaces between
        items are necessary.

        You can also enter the following commands:
        - "\help" to see this list of commands
        - "\quit" or "\q" to quit
        - "\history" to see the last few URLs you entered
        - "\update-downloader" or "\update-dl" to update yt-dlp using the command in your settings
          (If you start experiencing constant download errors, try this command)
        - Modify the current session only (without updating the settings file):
            - `\split` toggles chapter splitting
            - `\images` toggles image embedding
            - `\quiet` toggles quiet mode
            - `\format-` followed by a supported audio format (e.g., `\format-m4a`) changes the format
            - `\quality-` followed by a supported audio quality (e.g., `\quality-0`) changes the audio quality

        Enter `\commands` to see this summary in the application.

        Reporting issues:

        If you run into any issues, feel free to create an issue on GitHub. Please provide as much
        information as possible (e.g., entered URLs, system information, yt-dlp version).
        """
