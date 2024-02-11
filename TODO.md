# TODOs and Ideas

Features that I might consider as features or improvements in the future. Not all ideas might be viable, or even _good_, but this helps me avoid forgetting them until I decide what to do.

- Add a post-processing–only option for already-downloaded temporary files (via aborted downloads, etc.)
- Stop downloading if there are repeated errors from yt-dlp
- Logging to a file
- Clean up errors (e.g., add a summary header at the caller and remove from the `return`)
- JSON history that stores more data than just URLs (Or maybe the log file would be sufficient? If so, is the history file even needed? Maybe allow up-and-down scrolling through history?)
- yt-dlp can handle tabs on YouTube channels too, so look into supporting those
- Changing, saving, and reloading of settings while running the program
- Save errors to a file with details
- Possible to include yt-dlp itself as a binary? (Even if so, does it make sense to?)
- Count generated files for final output
- Set up actual releases (likely once .NET 8 is released)
- Add audio normalization, maybe (I need to find a command line tool or NuGet package that works with M4A files)

## Rejected

(None yet.)
