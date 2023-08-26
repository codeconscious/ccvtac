# CCVTAC-C#

A small .NET CLI tool that acts as a wrapper around [yt-dlp](https://github.com/yt-dlp/yt-dlp) to even more easily download audio from specific YouTube videos.

I [originally created this tool in Ruby](https://github.com/codeconscious/youtube-audio-downloader-ruby) to get better acquainted with Ruby for work, but I've rewritten it in C# because I decided to move from MP3 to M4A files to avoid unnecessary conversion (for better quality in smaller files), but the Ruby gem (library) I was using doesn't support M4A; and frankly, I simply enjoy working with C# much more. (I might find a way to force F# in here too.)

Feel free to use it yourself, but please do so responsibly. No warranties or guarantees provided!

## Features

- Easily convert YouTube videos to local audio files!
- Supports video and playlist URLs
- Video metadata (uploader name and URL, source URL, etc.) saved to Comment tags
- Renames files via specific regex patterns (to remove resource IDs, etc.)
- Video thumbnails are auto-trimmed and written to files as album art
- Post-processed files are automatically moved to a specified directory
- All URLs entered are saved locally to a file named `history.log`

### TODOs and Ideas

- Add audio normalization
- Add a post-processing–only option for already-downloaded temporary files (via aborted downloads, etc.)

## Running It

Prerequisites:

- .NET 7
- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- Optional: [mogrify](https://imagemagick.org/script/mogrify.php) (for auto-trimming album art)

Run the program with `dotnet run`. Pass `-h` or `--help` for the instructions.

If your `settings.json` file does not exist, it will created in the application directory with default settings. At minimum, you will need to enter paths to two existing directories: (1) a working directory for temporary files and (2) the directory to which the final audio files should be moved.

Once the program is running, simply enter a YouTube video or playlist URL at the prompt and press Enter.

Recommended: Periodically ensure you're running the latest version of yt-dlp using `sudo yt-dlp -U`.
