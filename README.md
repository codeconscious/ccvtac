# CCVTAC

CCVTAC (CodeConscious Video-to-Audio Converter) is a small .NET CLI tool that acts as a wrapper around [yt-dlp](https://github.com/yt-dlp/yt-dlp) to enable even easier downloads of M4A audio from specific YouTube videos, playlist, and channels, plus do some automatic post-processing (tagging and moving) as well.

Feel free to use it yourself, but please do so responsibly. No warranties or guarantees provided!

## Features

- Converts YouTube videos to local M4A audio files (mostly thanks to [yt-dlp](https://github.com/yt-dlp/yt-dlp))
- Supports video, playlist, and channel URLs
- Adds video metadata (uploader name and URL, source URL, etc.) to Comment tags
- ID3 tags are automatically written where possible
- Renames files via specific regex patterns (to remove resource IDs, etc.)
- Optionally auto-trims and writes video thumbnails to files as album art
- Moves files to a specified directory
- Saves entered URLs locally to a history file
- Allows customized behavior via a user settings file

### TODOs and Ideas

- Add audio normalization (I need to find a command line tool or NuGet package that works with M4A files)
- Add a post-processing–only option for already-downloaded temporary files (via aborted downloads, etc.)

## Running It

Prerequisites:

- .NET 7 (until I perhaps get some proper releases ready)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- Optional: [mogrify](https://imagemagick.org/script/mogrify.php) (for auto-trimming album art)

Run the program with `dotnet run`. Pass `-h` or `--help` for the instructions.

If your `settings.json` file does not exist, it will created in the application directory with default settings. At minimum, you will need to enter paths to two existing directories: (1) a working directory for temporary files and (2) the directory to which the final audio files should be moved. The other settings are optional.

Once the program is running, simply enter a YouTube video, playlist, or channel URL at the prompt and press Enter.

Recommended: Periodically ensure you're running the latest version of yt-dlp using `sudo yt-dlp -U` (or whatever command is appropriate for your system).
