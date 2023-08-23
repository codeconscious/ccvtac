# CCVTAC-CSharp

A small .NET CLI tool that acts as a wrapper around [yt-dlp](https://github.com/yt-dlp/yt-dlp) to even more easily download audio from specific YouTube videos.

I [originally created this tool in Ruby](https://github.com/codeconscious/youtube-audio-downloader-ruby) to get better acquainted with Ruby for work, but I've rewritten it in C# because I decided to move from MP3 to M4A files to avoid unnecessary conversion, but the Ruby gem (library) I was using doesn't support M4A; and frankly, while Ruby's neat, I enjoy working with C# more.

Feel free to use it yourself, but please do so responsibly. No warranties or guarantees provided!

## Features

- Easily convert YouTube videos to local M4A audio files!
- Supports video and playlist URLs
- Video metadata saved to Comment tags
- Renames files via
- Video thumbnails are auto-trimmed and written to files as album art
- Files are automatically moved to a specified directory
- All URLs entered are saved locally to a file named `history.log`

### TODOs and Ideas

- Add audio normalization
- Add a post-processing–only option for already-downloaded temporary files (via aborted downloads, etc.)

## Running It

Prerequisites:

- .NET 7
- yt-dlp

Run the program with `dotnet run`. Pass `-h` or `--help` for the instructions.

If your `settings.json` file does not exist, it will created in the application directory. You will need to enter paths to two existing directories: (1) a working directory for temporary files and (2) the directory to which the final audio files should be moved.

Once the program is running, simply enter a YouTube video or playlist URL at the prompt and press Enter.

Recommended: Periodically ensure you're running the latest version of yt-dlp using `sudo yt-dlp -U`.
