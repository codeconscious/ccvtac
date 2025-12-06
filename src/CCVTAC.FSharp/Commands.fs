namespace CCVTAC.Console

open System
open System.Collections.Generic

module Commands =

    let prefix = '\\'

    let private makeCommand text : string =
        if String.hasNoText text then
            raise (ArgumentException("Commands cannot be null or white space.", "text"))
        if text.Contains ' ' then
            raise (ArgumentException("Commands cannot contain white space.", "text"))
        $"%c{prefix}%s{text}"

    let quitCommands: string[] =
        [| makeCommand "quit"; makeCommand "q"; makeCommand "exit" |]

    let helpCommand: string = makeCommand "help"

    let settingsSummary: string[] = [| makeCommand "settings" |]

    let history: string[] = [| makeCommand "history" |]

    let updateDownloader: string[] =
        [| makeCommand "update-downloader"; makeCommand "update-dl" |]

    let splitChapterToggles: string[] = [| makeCommand "split"; makeCommand "toggle-split" |]

    let embedImagesToggles: string[] = [| makeCommand "images"; makeCommand "toggle-images" |]

    let quietModeToggles: string[] = [| makeCommand "quiet"; makeCommand "toggle-quiet" |]

    let updateAudioFormatPrefix: string = makeCommand "format-"

    let updateAudioQualityPrefix: string = makeCommand "quality-"

    let summary: Dictionary<string, string> =
        let d = Dictionary<string, string>()
        d.Add(String.Join(" or ", history), "See the most recently entered URLs")
        d.Add(String.Join(" or ", splitChapterToggles), "Toggles chapter splitting for the current session only")
        d.Add(String.Join(" or ", embedImagesToggles), "Toggles image embedding for the current session only")
        d.Add(String.Join(" or ", quietModeToggles), "Toggles quiet mode for the current session only")
        d.Add(String.Join(" or ", updateDownloader), "Updates the downloader using the command specified in the settings")
        d.Add(updateAudioFormatPrefix,
              sprintf "Followed by a supported audio format (e.g., %sm4a), changes the audio format for the current session only"
                  updateAudioFormatPrefix)
        d.Add(updateAudioQualityPrefix,
              sprintf "Followed by a supported audio quality (e.g., %s0), changes the audio quality for the current session only"
                  updateAudioQualityPrefix)
        d.Add(String.Join(" or ", quitCommands), "Quit the application")
        d.Add(helpCommand, "See this help message")
        d
