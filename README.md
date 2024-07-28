# CCVTAC

CCVTAC (CodeConscious Video-to-Audio Converter) is a small .NET-powered CLI tool that acts as a wrapper around [yt-dlp](https://github.com/yt-dlp/yt-dlp) to enable easier download and extractions of M4A audio from YouTube videos, playlists, and channels, plus do some automatic post-processing (tagging, renaming, and moving).

<img width="1451" alt="Sample download" src="https://github.com/codeconscious/ccvtac/assets/50596087/40fd5c56-0c39-44c4-9f5e-bc6398337820">

While I maintain it primarily for my own use, feel free to use it yourself. No warranties or guarantees provided!

[![Build and test](https://github.com/codeconscious/ccvtac/actions/workflows/build-test.yml/badge.svg)](https://github.com/codeconscious/ccvtac/actions/workflows/build-test.yml)

## Features

- Converts YouTube videos to local M4A audio files (via [yt-dlp](https://github.com/yt-dlp/yt-dlp))
- Supports 5 kinds of downloads
  - Video
  - Video on a playlist
  - Standard playlist (generally with the newest video at index 1)
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
  - Apply rules for detecting tag data
  - Apply rules for auto-renaming
  - Set sleep times between batches (multiple URLs entered at once) and individual video downloads
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

If your `settings.json` file does not exist, one will be created in the application directory with default settings. At minimum, you will need to enter (1) an existing directory for temporary working files, (2) an existing directory to which the final audio files should be moved, and (3) a path to your history file. The other settings have sensible defaults.

#### Sample file with explanatory comments

```
{
  # Mandatory. A temporary directory for working files.
  # Cleared after processing a batch (i.e., URL).
  "workingDirectory": "/Users/me/temp",

  # Mandatory. Where final audio files should be saved.
  "moveToDirectory": "/Users/me/Downloads",

  # Mandatory. A local history of all URLs entered.
  "historyFile": "/Users/me/Downloads/history.log",

  # Count of entries to show for `history` command
  "historyDisplayCount": 20,

  # Split videos with chapters into separate files?
  "splitChapters": true,

  # Delay in seconds between individual video downloads for
  # playlists and channels. Use to avoid slamming YouTube servers
  # with several downloads in succession.
  "sleepSecondsBetweenDownloads": 10,

  # Delay in seconds between batches (i.e., each URL entered).
  # Use to avoid slamming YouTube servers with several downloads
  # in succession.
  "sleepSecondsBetweenBatches": 20,

  # Whether output should be verbose (true) or quiet (false).
  "verboseOutput": true,

  # Embed video thumbnails into file tags?
  "embedImages": true,

  # Channel names for which the video thumbnail should
  # never be embedded in the audio file.
  "doNotEmbedImageUploaders": [
    "Channel Name",
    "Another Channel Name"
  ],

  # By default, the upload year of the video is
  # saved to files' Year tag. However, this will
  # not occur for videos on channels listed here.
  "ignoreUploadYearUploaders": [
    "Channel Name",
    "Another Channel Name"
  ],

  # Rules for detecting tag data from video metadata.
  "tagDetectionPatterns": {

    # Currently supports 5 tags -- this one (Title) and its siblings.
    "title": [
      {
        # A regex pattern for searching in the video metadata field specified below.
        "regex": "(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\\d{3})\\D",

        # Use the text that comprises this match group number.
        # `1` and greater indicates the specified group. You must use groups in the regex pattern!
        # `0` indicates the entirety of the match text.
        "matchGroup": 1,

        # Which video metadata field should be searched, `title` or `description`?
        "searchField": "description",

        # An arbitrary name for the rule. It will appear in the output when this pattern is matched.
        "summary": "Topic style"
      }
    ],

    # The same data format is applicable to these tags as well.
    "artist": [],
    "album": [],
    "composer": [],
    "year": []
  },

  # Rules for auto-renaming audio files.
  "renamePatterns": [
    {
      # Regular expression that matches some or all of a filename.
      "regex": "\\s\\[[\\w_-]{11}\\](?=\\.\\w{3,5})",

      # What the matched text should be replaced with.
      "replacePattern": "",

      # Friendly summary to display in the output (if verbose output is on).
      "description": "Remove trailing video IDs"
    },
    {
      # Optionally use regex groups to match specific substrings.
      # The matched groups will replace numbered placeholders (of
      # the format `%<#>s`) in the replacement patterns!
      # (The placeholder numbers must match the regex groups'.)
      "regex": "【(.+)】(.+)",
      "replacePattern": "%<1>s - %<2>s",
      "description": "Change `【artist】title` to `ARTIST - TRACK`"
    },
  ]
}
```

#### Starting template

Below is a mostly-empty settings you can copy and save to `settings.json` to get started. Be sure to fill out the first three entries!

```json
{
  "workingDirectory": "",
  "moveToDirectory": "",
  "historyFile": "",
  "historyDisplayCount": 20,
  "splitChapters": true,
  "sleepSecondsBetweenDownloads": 10,
  "sleepSecondsBetweenBatches": 20,
  "verboseOutput": true,
  "embedImages": true,
  "doNotEmbedImageUploaders": [
    "Channel Name 1",
    "Channel Name 2"
  ],
  "ignoreUploadYearUploaders": [
    "Channel Name 1",
    "Channel Name 2"
  ],
  "tagDetectionPatterns": {
    "title": [
      {
        "regex": "(.+?) · (.+)(?:\n|\r|\r\n){2}(.+)(?:\n|\r|\r\n){2}.*℗ ([12]\\d{3})\\D",
        "matchGroup": 1,
        "searchField": "description",
        "summary": "Topic style"
      }
    ],
    "artist": [],
    "album": [],
    "composer": [],
    "year": []
  },
  "renamePatterns": [
    {
      "regex": "\\s\\[[\\w_-]{11}\\](?=\\.\\w{3,5})",
      "replacePattern": "",
      "description": "Remove trailing video IDs (recommend running this first)"
    }
  ]
}
```

### Using the application

Once your settings file is ready, run the application with `dotnet run`. Optionally, pass `-h` or `--help` for instructions (e.g., `dotnet run -- --help`).

When the application is running, simply enter at least one YouTube media URL (video, playlist, or channel) at the prompt and press the Enter key. You can omit spaces between the URLs.

Enter `!quit` or `!q` to quit.

Entering `!history` will display your recent URL history.

## Upgrading yt-dlp

Periodically ensure you are running the latest version of yt-dlp, especially if you start experiencing download errors. See the [yt-dlp GitHub page](https://github.com/yt-dlp/yt-dlp#update) for more. (Likely commands are `sudo yt-dlp -U` or `pip install --upgrade yt-dlp`.)

## Reporting issues

If you run into any issues, feel free to create an issue on GitHub with as much information as possible (e.g., entered URLs, system information, yt-dlp version).
