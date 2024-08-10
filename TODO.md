# TODOs

Features and improvements I'm considering in the future. Not all ideas might be viable, or even _good_, but this helps me avoid forgetting them until I decide what to do.

## Definitely

- Review auto-adding album art (because even if it is deleted from files, the padding remains and the file is not immediately made smaller)
- Stop downloading if there are repeated errors from yt-dlp
- When creating a blank settings file, include all options

## Probably

- Logging to files
- Add a post-processing–only option for already-downloaded temporary files (via aborted downloads, etc.)
- Changing, saving, and reloading of settings while running the program
- Add audio normalization (once I find a suitable command line tool or NuGet package)
- Disable image cropping when adding images is disabled in the settings.

## Maybe

- Lower-quality audio setting?
- Include yt-dlp internally
  - See https://www.reddit.com/r/rust/comments/11jfkw6/how_to_build_a_rust_app_that_uses_ytdlp/jb2tf46/
- Change settings file format from JSON to YAML?
- JSON history that stores more data than just URLs (Or maybe the log file would be sufficient? If so, is the history file even needed? Maybe allow up-and-down scrolling through history?)
- yt-dlp can handle tabs on YouTube channels too, so add support for those
- Count generated files for final output
- Set up actual releases on GitHub, ideally using GitHub Actions
- Output the counts of URL types too (e.g., "# videos, # playlists")
- Don't download images when adding images is disabled in the settings
- Bug: Leftover temp files if the same video exists twice in a playlist. (So uncommon, maybe not worth fixing.)

## Rejected

- Save errors to a file with details
