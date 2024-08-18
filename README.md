# CCVTAC

CCVTAC (CodeConscious Video-to-Audio Converter) is a small .NET-powered CLI tool that acts as a wrapper around [yt-dlp](https://github.com/yt-dlp/yt-dlp) to enable easier download and extractions of audio from YouTube videos, playlists, and channels, plus do some automatic post-processing (tagging, renaming, and moving).

While I maintain it primarily for my own use, feel free to use it yourself. No warranties or guarantees are provided.

[![Build and test](https://github.com/codeconscious/ccvtac/actions/workflows/build-test.yml/badge.svg)](https://github.com/codeconscious/ccvtac/actions/workflows/build-test.yml)

## Features

- Converts YouTube videos, playlists, and channels to local audio files (via [yt-dlp](https://github.com/yt-dlp/yt-dlp))
- Writes ID3 tags to files where possible using available or regex-detected metadata
- Adds video metadata (channel name and URL, video URL, etc.) to files' Comment tags
- Auto-renames files via custom regex patterns (to remove media IDs, etc.)
- Optionally writes video thumbnails to files as artwork (if [mogrify](https://imagemagick.org/script/mogrify.php) is installed)
- Customized behavior via a user settings file — e.g., chapter splitting, image embedding, directories
- Saves entered URLs to a local history file

## Prerequisites

- [.NET 8 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [ffmpeg](https://ffmpeg.org/) (for yt-dlp artwork extraction)
- Optional: [mogrify](https://imagemagick.org/script/mogrify.php) (for auto-trimming album art)

## Screenshots

### Normal output

<img width="1512" alt="ccvtac" src="https://github.com/user-attachments/assets/6d4020a5-5db0-4904-bdf9-cd668f1d60f3">

### Quiet mode

![ccvtac-quiet](https://github.com/user-attachments/assets/382785d1-f313-42ae-8ca3-afeaf25cd357)

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

  // The audio format (codec) to extract audio to.
  // Options: best, aac, alac, flac, m4a, mp3, opus, vorbis, wav
  // Not all options are available for all videos.
  "audioFormat": "default",

  // The audio quality to use, with 10 being the lowest and 0 being the highest.
  "audioQuality": 0,

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
- `\history` to see the URLs you most recently entered
- Modify the current session only (without updating the settings file):
  - `\split` toggles chapter splitting
  - `\images` toggles image embedding
  - `\quiet` toggles quiet mode
  - `\format-` followed by a supported audio format (e.g., `\format-m4a`) changes the format
  - `\quality-` followed by a supported audio quality (e.g., `\quality-0`) changes the audio quality

## Upgrading yt-dlp

Periodically ensure you are running the latest version of yt-dlp, especially if you start experiencing download errors. See the [yt-dlp GitHub page](https://github.com/yt-dlp/yt-dlp#update) for more. (Likely commands are `sudo yt-dlp -U` or `pip install --upgrade yt-dlp`.)

## Reporting issues

If you run into any issues, please create an issue on GitHub with as much information as possible (e.g., entered URLs, OS, .NET version, yt-dlp version, etc.).
