# CCVTAC

CCVTAC (CodeConscious Video-to-Audio Converter) is a small .NET CLI tool that acts as a wrapper around [yt-dlp](https://github.com/yt-dlp/yt-dlp) to enable even easier downloads of M4A audio from specific YouTube videos, playlist, and channels, plus do some automatic post-processing (tagging and moving) as well.

Feel free to use it yourself, but please do so responsibly. No warranties or guarantees provided!

## Features

- Converts YouTube videos to local M4A audio files (via [yt-dlp](https://github.com/yt-dlp/yt-dlp))
- Supports video, playlist, and channel URLs
- Adds video metadata (channel name, channel URL, video URL, etc.) summary to Comment tags
- ID3 tags (artists, title, etc.) are automatically written where possible (via metadata or regex-based detection)
- Auto-renames files via specified regex patterns (to remove resource IDs, etc.)
- Optionally auto-trims and writes video thumbnails to files as album art (if [mogrify](https://imagemagick.org/script/mogrify.php) is installed)
- Customizes behavior via a user settings file
  - Optionally split video chapters
  - Specify the working directory for temporary files
  - Specify an output directory for final audio files
  - List channels for whom video upload years should _not_ be added to the tags' Year field (Adding years is the default behavior)
  - Sleep times between batches (multiple URLs entered at once) and individual video downloads
- Saves entered URLs locally to a history file

## Running It

Prerequisites:

- .NET 7 (until I perhaps get some proper releases ready, likely once .NET 8 is released)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- Optional: [mogrify](https://imagemagick.org/script/mogrify.php) (for auto-trimming album art)

Run the program with `dotnet run`. Pass `-h` or `--help` for the instructions.

If your `settings.json` file does not exist, it will created in the application directory with default settings. At minimum, you will need to enter paths to two existing directories: (1) a working directory for temporary files and (2) the directory to which the final audio files should be moved. The other settings are optional.

Once the program is running, simply enter at least one YouTube video, playlist, or channel URL at the prompt and press Enter.

Recommended: Periodically ensure you're running the latest version of yt-dlp using `sudo yt-dlp -U` (or whatever command is appropriate for your system—see the [yt-dlp GitHub page](https://github.com/yt-dlp/yt-dlp#update) for more), especially if you start experiencing download errors.
