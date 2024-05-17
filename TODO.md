# TODOs

Features and improvements I'm considering in the future. Not all ideas might be viable, or even _good_, but this helps me avoid forgetting them until I decide what to do.

## Definitely

- Review audo-adding album art (because even if it is deleted, the padding remains and the file is not made smaller)
- Add a post-processing–only option for already-downloaded temporary files (via aborted downloads, etc.)
- Stop downloading if there are repeated errors from yt-dlp

## Probably

- Logging to files
- Changing, saving, and reloading of settings while running the program
- Add audio normalization (once I find a command line tool or NuGet package that works with M4A files)

## Maybe

- Include yt-dlp internally
  - https://www.reddit.com/r/rust/comments/11jfkw6/how_to_build_a_rust_app_that_uses_ytdlp/jb2tf46/
- Change settings file format from JSON to YAML?
- Save errors to a file with details
- JSON history that stores more data than just URLs (Or maybe the log file would be sufficient? If so, is the history file even needed? Maybe allow up-and-down scrolling through history?)
- yt-dlp can handle tabs on YouTube channels too, so look into supporting those
- Count generated files for final output
- Set up actual releases on GitHub (using Actions?)
- Output the counts of URL types too (e.g., "# videos, # playlists")

## Rejected

None yet.
