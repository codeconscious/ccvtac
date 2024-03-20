# CCVTAC

CCVTAC (CodeConscious Video-to-Audio Converter) is a small .NET-powered CLI tool that acts as a wrapper around [yt-dlp](https://github.com/yt-dlp/yt-dlp) to enable easier downloads of M4A audio from YouTube videos, playlist, and channels, plus do some automatic post-processing (tagging, renaming, and moving) as well.

<img width="1451" alt="Sample download" src="https://github.com/codeconscious/ccvtac/assets/50596087/40fd5c56-0c39-44c4-9f5e-bc6398337820">

Feel free to use it yourself, but please do so responsibly. No warranties or guarantees provided!

## Features

- Converts YouTube videos to local M4A audio files (via [yt-dlp](https://github.com/yt-dlp/yt-dlp))
- Supports 5 kinds of downloads
  - Video
  - Video on a playlist
  - Standard playlist (with the newest video at index 1)
  - Release playlist (in which the playlist index represents the album track number)
  - Channel
- Writes ID3 tags (artists, title, etc.) to files where possible (via metadata or regex-based detection)
- Adds limited video metadata (channel name and URL, video URL, etc.) summary to files' Comment tags
- Auto-renames files via specified regex patterns (to remove resource IDs, etc.)
- Optionally auto-trims and writes video thumbnails to files as album art (if [mogrify](https://imagemagick.org/script/mogrify.php) is installed)
- Customized behavior via a user settings file
  - Optionally split video chapters into separate files
  - Specify the working directory for temporary files
  - Specify an output directory for audio files
  - Specify channels for whom video upload years should _not_ be added to the tags' Year field (Adding the years is the default behavior)
  - Set sleep times between batches (multiple URLs entered at once) and individual video downloads
- Saves entered URLs to a local history file

## Running It

### Prerequisites

- [.NET 8 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- A valid settings file (see below)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- Optional: [mogrify](https://imagemagick.org/script/mogrify.php) (for auto-trimming album art)

### Settings

A valid settings file is mandatory to use this application.

The application will look for a file named `settings.json` in its directory. However, you can manually specify an existing file path using the `-s` option, such as `dotnet run -- -s <PATH_TO_YOUR_FILE>`.

If your `settings.json` file does not exist, one will be created in the application directory with default settings. At minimum, you will need to enter (1) an existing directory for temporary working files, (2) an existing directory to which the final audio files should be moved, and (3) a path to your history file. The other settings have sensible defaults.

Sample settings file:

```json
{
  "workingDirectory": "/Users/me/temp", // A temporary home for working files
  "moveToDirectory": "/Users/me/Downloads", // Where audio files should be saved
  "historyFile": "/Users/me/Downloads/history.log", // Log for your entered URLs
  "sleepSecondsBetweenDownloads": 10, // Delay between video downloads (for playlists and channels)
  "sleepSecondsBetweenBatches": 20, // Delay between batches (i.e., each URL entered)
  "splitChapters": true, // Split videos with chapters into separate files?
  "historyDisplayCount": 20, // Count of entries to show for `history` command
  "verboseOutput": true, // Use `false` for quiet mode
  "ignoreUploadYearUploaders": [ // By default, the upload year of the video is
    "Channel Name",              // saved to files' Year tag. However, this will
    "Another channel name"       // not occur for videos on channels listed here.
  ]
}
```

I added the `sleepSecondsBetweenDownloads` and `sleepSecondsBetweenBatches` settings to help reduce concentrated loads on YouTube servers. Please avoid lowering these values too much and slamming their servers with enormous, long-running downloads (even if their servers can take it).

### Using the application

Once your settings are ready, run the application with `dotnet run`. Optionally, pass `-h` or `--help` for instructions (e.g., `dotnet run -- --help`).

When the application is running, simply enter at least one YouTube media URL (video, playlist, or channel) at the prompt and press the Enter key. Separate multiple URLs by spaces.

Enter `quit` or `q` to quit.

Entering `history` will display your recent URL history.

## Upgrading yt-dlp

Periodically ensure you are running the latest version of yt-dlp, especially if you start experiencing download errors. See the [yt-dlp GitHub page](https://github.com/yt-dlp/yt-dlp#update) for more. (Likely commands are `sudo yt-dlp -U` or `pip install --upgrade yt-dlp`.)

## Reporting issues

If you run into any issues, please create an issue on GitHub with as much information as possible. Thank you!
