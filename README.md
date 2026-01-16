# CCVTAC

CCVTAC (CodeConscious Video-to-Audio Converter) is a small .NET-powered CLI tool written in F# that acts as a wrapper around [yt-dlp](https://github.com/yt-dlp/yt-dlp) to enable easier download and extractions of audio from YouTube videos, playlists, and channels, plus do some automatic post-processing (tagging, renaming, and moving).

Feel free to use it yourself, but please note that it's geared to my personal use case and that no warranties or guarantees are provided.

[![Build and test](https://github.com/codeconscious/ccvtac/actions/workflows/build-test.yml/badge.svg)](https://github.com/codeconscious/ccvtac/actions/workflows/build-test.yml)

## Features

- Converts YouTube videos, playlists, and channels to local audio files (via [yt-dlp](https://github.com/yt-dlp/yt-dlp))
- Writes ID3 tags to files where possible using available metadata via regex-based detection
- Logs video metadata (channel name and URL, video URL, etc.) to files' Comments tags
- Auto-renames files via custom regex patterns (to remove video IDs, etc.)
- Optionally writes video thumbnails to files as artwork (if [mogrify](https://imagemagick.org/script/mogrify.php) is installed)
- Customizable behavior via a settings file
- Saves entered URLs to a local history file

## Prerequisites

- [.NET 10 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [yt-dlp](https://github.com/yt-dlp/yt-dlp)
- [ffmpeg](https://ffmpeg.org/) (for yt-dlp artwork extraction and conversion)
- Optional: [mogrify](https://imagemagick.org/script/mogrify.php) (for auto-trimming album art)

## Screenshots

### Quiet mode

![ccvtac-quiet](https://github.com/user-attachments/assets/382785d1-f313-42ae-8ca3-afeaf25cd357)

### Normal mode

<img width="1512" alt="ccvtac" src="https://github.com/user-attachments/assets/6d4020a5-5db0-4904-bdf9-cd668f1d60f3">

## Running It

### Settings

A valid JSON settings file is mandatory to use this application.

By default, the application will look for a file named `settings.json` in its directory. However, you can manually specify an existing file path using the `-s` option, such as `dotnet run -- -s <PATH_TO_YOUR_FILE>`.

> [!TIP]
> The `--` is necessary to indicate that the command and arguments are for this program and not for `dotnet`.

If your `settings.json` file does not exist, a default one will be created. At minimum, you will need to enter (1) an existing directory for temporary working files, (2) an existing directory to which the final audio files should be moved, and (3) a path to your history file. The other settings have sensible defaults. Some settings require familiarity with regular expressions (regex).

<details>
  <summary>Click to see a sample settings file</summary>

The sample below contains explanations and some example values as well.

**Important:** When entering regular expressions, you must double-up backslashes. For example, to match a whitespace character, use `\\s` instead of `\s`.

```js
{
  // Mandatory. The working directory for temporary files.
  // It is emptied after processing each URL.
  "workingDirectory": "/Users/me/temp",

  // Mandatory. The directory in which final audio files should be saved.
  "moveToDirectory": "/Users/me/Downloads",

  // Mandatory. A local file containing the history of all URLs entered.
  "historyFile": "/Users/me/Downloads/history.log",

  // Count of entries to show for `history` command.
  "historyDisplayCount": 20,

  // The audio formats (codec) audio should be extracted to.
  // Options: best, aac, alac, flac, m4a, mp3, opus, vorbis, wav.
  // Not all options are available for all videos.
  "audioFormats": ["m4a", "best"],

  // The audio quality to use, with 10 being the lowest and 0 being the highest.
  "audioQuality": 0,

  // Split videos with chapters into separate files?
  "splitChapters": true,

  // Embed video thumbnails into file tags?
  "embedImages": true,

  // Whether to use quiet mode (true) or not (false).
  // Fewer details are shown in quiet mode.
  "quietMode": false,

  // Delay in seconds between individual video downloads for
  // playlists and channels. Use to avoid burdening YouTube servers
  // and getting rate-limited.
  "sleepSecondsBetweenDownloads": 10,

  // Delay in seconds between each URL entered in a batch.
  // Use to avoid burdening YouTube servers and getting rate-limited.
  "sleepSecondsBetweenURLs": 20,

  // The Unicode normalization form to use for filenames.
  // Valid values are `C`, `D`, `KC`, and `KD`.
  // `C` is used by default if no valid value is provided.
  //
  // Reference: https://unicode.org/reports/tr15/
  // Reference: https://en.wikipedia.org/wiki/Unicode_equivalence
  "normalizationForm": "C",

  // The full command you use to update your local yt-dlp installation.
  // This is a sample entry.
  "downloaderUpdateCommand": "pip install --upgrade yt-dlp", 

  // Arbitrary yt-dlp options to be included in all yt-dlp commands.
  // Use with caution, as some options could disrupt operation of this program.
  // Intended to be used only when necessary to resolve download issues.
  // For example, see https://github.com/yt-dlp/yt-dlp/wiki/EJS,
  // upon which this sample is based.
  "downloaderAdditionalOptions": "--remote-components ejs:github",

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

    // Currently supports 5 tags: this one (title) and its siblings listed below.
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

        // An arbitrary summary of the rule. If quiet mode is off, this name will appear
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

Once your settings file is ready, run the application with `dotnet run` within the `CCVTAC.Console` directory, optionally passing the path to your settings file using `-s`. Alternatively, pass `-h` or `--help` for instructions (e.g., `dotnet run -- --help`).

When the application is running, enter at least one YouTube media URL (video, playlist, or channel) or command at the prompt and press Enter. No spaces between items are necessary.

List of commands:
- `\help` to see this list of commands
- `\quit` or `\q` to quit
- `\history` to see the URLs you most recently entered
- `\update-downloader` or `\update-dl` to update yt-dlp using the command in your settings (Note: If you start experiencing constant download errors, try this command to ensure you have the latest version)
- Modify the current session only (without updating the settings file):
  - `\split` toggles chapter splitting
  - `\images` toggles image embedding
  - `\quiet` toggles quiet mode
  - `\format-` followed by a supported audio format (e.g., `\format-m4a`) changes the audio format
  - `\quality-` followed by a supported audio quality (e.g., `\quality-0`) changes the audio quality

Enter `\commands` in the application to see this summary.

## Reporting issues

If you run into any issues, feel free to create an issue on GitHub. Please provide as much information as possible (i.e., entered URLs or comments, system information, yt-dlp version, etc.) and I'll try to take a look.

However, do keep in mind that this is ultimately a hobby project for myself, so I cannot guarantee every issue will be fixed.

## History

The first incarnation of this application was written in C#. However, after picking up [F#](https://fsharp.org/) out of curiosity about it and functional programming (FP) in 2024 and successfully using it to create other tools (mainly [Audio Tag Tools](https://github.com/codeconscious/audio-tag-tools/)) in an FP style, I become curious about F#'s OOP capabilities as well.

As an experiment, I rewrote this application in OOP-style F#, using LLMs solely for the rough initial conversion (which greatly reduced the overall time and labor necessary at the cost of requiring a *lot* of manual cleanup). Ultimately, I was surprised how much I preferred the F# code over the C#, so I decided to keep this tool in F#.

Due to this background, the code is not particularly idiomatic F#, but it is certainly perfectly viable in its current blended-style form. That said, I'll probably tweak it over time to gradually to introduce more idiomatic F# code.
