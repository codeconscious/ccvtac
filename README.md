# CCVTAC

CCVTAC (CodeConscious Video-to-Audio Converter) is a small .NET-powered CLI tool that acts as a wrapper around [yt-dlp](https://github.com/yt-dlp/yt-dlp) to enable easier downloads of M4A audio from YouTube videos, playlist, and channels, plus do some automatic post-processing (tagging, renaming, and moving) as well.

<img width="1451" alt="Sample download" src="https://github.com/codeconscious/ccvtac/assets/50596087/40fd5c56-0c39-44c4-9f5e-bc6398337820">

Feel free to use it yourself, but please do so responsibly. No warranties or guarantees provided!

## Features

- Converts YouTube videos to local M4A audio files (via [yt-dlp](https://github.com/yt-dlp/yt-dlp))
- Supports 5 kinds of downloads
  - Video
  - Video on a playlist (Playlist metadata will be written to the comments)
  - Standard playlist (with the newest video at index 1)
  - Release playlist (in which the playlist index represents the album number)
  - Channel
- Writes ID3 tags (artists, title, etc.) to files where possible (via metadata or regex-based detection)
- Adds limited video metadata (channel name, channel URL, video URL, etc.) summary to files' Comment tags
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

Prerequisites:

- [.NET 8 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- Optional: [mogrify](https://imagemagick.org/script/mogrify.php) (for auto-trimming album art)

Run the program with `dotnet run`. Optionally, pass `-h` or `--help` for the instructions.

If your `settings.json` file does not exist, it will created in the application directory with default settings. At minimum, you will need to enter (1) a directory for temporary working files, (2) a directory to which the final audio files should be moved, and (3) a path to your history file. The other settings are optional.

Once the program is running, simply enter at least one YouTube media URL at the prompt and press the Enter key. Enter `quit` or `q` to quit. Entering `history` will show your recent URL history.

*Recommended:* Periodically ensure you're running the latest version of yt-dlp, especially if you start experiencing download errors. See the [yt-dlp GitHub page](https://github.com/yt-dlp/yt-dlp#update) for more. (Likely commands are `sudo yt-dlp -U` or `pip install --upgrade yt-dlp`.)

If you run into any issues, please create an issue on GitHub with as much information as possible.
