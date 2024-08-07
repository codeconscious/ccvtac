# CCVTAC

CCVTAC (CodeConscious Video-to-Audio Converter) is a small .NET-powered CLI tool that acts as a wrapper around [yt-dlp](https://github.com/yt-dlp/yt-dlp) to enable easier download and extractions of M4A audio from YouTube videos, playlists, and channels, plus do some automatic post-processing (tagging, renaming, and moving).

<img width="1451" alt="Sample download" src="https://github.com/codeconscious/ccvtac/assets/50596087/40fd5c56-0c39-44c4-9f5e-bc6398337820">

While I maintain it primarily for my own use, feel free to use it yourself. No warranties or guarantees are provided.

[![Build and test](https://github.com/codeconscious/ccvtac/actions/workflows/build-test.yml/badge.svg)](https://github.com/codeconscious/ccvtac/actions/workflows/build-test.yml)

## Features

- Converts YouTube videos to local M4A audio files (via [yt-dlp](https://github.com/yt-dlp/yt-dlp))
- Supports videos, playlists, and channels
- Writes ID3 tags to files where possible using available or regex-detected metadata
- Adds limited video metadata (channel name and URL, video URL, etc.) to files' Comment tags
- Auto-renames files via custom regex patterns (to remove media IDs, etc.)
- Optionally auto-trims and writes video thumbnails to files as album art (if [mogrify](https://imagemagick.org/script/mogrify.php) is installed)
- Customized behavior via a user settings file — e.g., chapter splitting, image embedding, directories
- Saves entered URLs to a local history file

## Prerequisites

- [.NET 8 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [ffmpeg](https://ffmpeg.org/) (for yt-dlp artwork extraction)
- Optional: [mogrify](https://imagemagick.org/script/mogrify.php) (for auto-trimming album art)

## Running It

### Settings

A valid settings file is mandatory to use this application.

By default, the application will look for a file named `settings.json` in its directory. However, you can manually specify an existing file path using the `-s` option, such as `dotnet run -- -s <PATH_TO_YOUR_FILE>`.

If your `settings.json` file does not exist, one will be created in the application directory with default settings. At minimum, you will need to enter (1) an existing directory for temporary working files, (2) an existing directory to which the final audio files should be moved, and (3) a path to your history file. The other settings have sensible defaults. Some settings require familiarity with regular expressions (regex).

#### Starter file with comments

You can copy and paste the sample settings file below to a JSON file named `settings.json` to get started. You will, in particular, need to update the three directories at the top. You can leave the commented lines as-is, as they will be ignored.

<details>
  <summary>Click here to expand!</summary>

```
{
  // Mandatory. The working directory for temporary files.
  // It is cleared after processing each URL.
  "workingDirectory": "/Users/me/temp",

  // Mandatory. The directory in which final audio files should be saved.
  "moveToDirectory": "/Users/me/Downloads",

  // Mandatory. A local file containing the history of all URLs entered.
  "historyFile": "/Users/me/Downloads/history.log",

  // Count of entries to show for `history` command
  "historyDisplayCount": 20,

  // Split videos with chapters into separate files?
  "splitChapters": true,

  // Delay in seconds between individual video downloads for
  // playlists and channels. Use to avoid burdening YouTube servers
  // with several downloads in quick succession.
  "sleepSecondsBetweenDownloads": 10,

  // Delay in seconds between batches (i.e., each URL entered).
  // Use to avoid burdening YouTube servers with several downloads
  // in quick succession.
  "sleepSecondsBetweenBatches": 20,

  # Whether to use quiet mode (true) or not (false).
  # Fewer details are shown in quiet mode.
  "quietMode": false,

  // Embed video thumbnails into file tags?
  "embedImages": true,

  // Channel names for which the video thumbnail should
  // never be embedded in the audio file.
  "doNotEmbedImageUploaders": [
    "Channel Name",
    "Another Channel Name"
  ],

  // By default, the upload year of the video is saved to files' Year tag.
  // However, this will not occur for videos on channels listed here.
  "ignoreUploadYearUploaders": [
    "Channel Name",
    "Another Channel Name"
  ],

  // Rules for detecting tag data from video metadata.
  // These require familiarity with regular expressions (regex).
  "tagDetectionPatterns": {

    // Currently supports 5 tags: this one (Title) and its siblings listed below.
    "title": [
      {
        // A regex pattern for searching in the video metadata field specified below.
        "regex": "(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\\d{3})\\D",

        // Specify the number of match group whose text should be used.
        // `1` and greater indicates a group number. In this case, you must specify groups in the regex pattern!
        // `0` indicates the entirety of the matched text. In this case, specifying groups is unnecessary.
        "matchGroup": 1,

        // Which video metadata field should be searched, `title` or `description`?
        "searchField": "description",

        // An arbitrary summary to the rule. If quiet mode is off, this name will appear
        // in the output when this pattern is matched.
        "summary": "Topic style"
      }
    ],

    // The same format is applicable to these tags as well.
    "artist": [],
    "album": [],
    "composer": [],
    "year": []
  },

  // Rules for auto-renaming audio files.
  "renamePatterns": [
    {
      // Regular expression that matches some or all of a filename.
      // This one matches the 11-digit media ID and surrounding
      // square brackets in downloaded filenames.
      "regex": "\\s\\[[\\w_-]{11}\\](?=\\.\\w{3,5})",

      // What the matched text should be replaced with.
      // `""` indicates that the matched text should simply be removed.
      "replacePattern": "",

      // An arbitrary summary to the rule. If quiet mode is off, this name will appear
      // in the output when this pattern is matched.
      "description": "Remove trailing video IDs"
    },
    {
      // Optionally use regex groups to match specific substrings.
      // The matched groups will replace numbered placeholders (of
      // the format `%<#>s`) in the replacement patterns!
      // The placeholder numbers must match the regex group numbers.
      "regex": "【(.+)】(.+)",
      "replacePattern": "%<1>s - %<2>s",
      "description": "Change `【artist】title` to `ARTIST - TRACK`"
    },
  ]
}
```
</details>

### Using the application

Once your settings file is ready, run the application with `dotnet run`. Optionally, pass `-h` or `--help` for instructions (e.g., `dotnet run -- --help`).

When the application is running, simply enter at least one YouTube media URL (video, playlist, or channel) at the prompt and press the Enter key. You can omit spaces between the URLs.

You can also enter the following commands:
- `\quit` or `\q` to quit
- `\history` to see the last few URLs you entered
- `\split` to toggle chapter splitting for the current session only
- `\images` to toggle image embedding for the current session only
- `\quiet` to toggle quiet mode for the current session only

## Upgrading yt-dlp

Periodically ensure you are running the latest version of yt-dlp, especially if you start experiencing download errors. See the [yt-dlp GitHub page](https://github.com/yt-dlp/yt-dlp#update) for more. (Likely commands are `sudo yt-dlp -U` or `pip install --upgrade yt-dlp`.)

## Reporting issues

If you run into any issues, please create an issue on GitHub with as much information as possible (e.g., entered URLs, OS, .NET version, yt-dlp version, etc.).
