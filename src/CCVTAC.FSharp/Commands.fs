namespace CCVTAC.Console

open System

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

    let summary: Map<string, string> =
        [
            String.Join(" or ", history), "See the most recently entered URLs"
            String.Join(" or ", splitChapterToggles), "Toggles chapter splitting for the current session only"
            String.Join(" or ", embedImagesToggles), "Toggles image embedding for the current session only"
            String.Join(" or ", quietModeToggles), "Toggles quiet mode for the current session only"
            String.Join(" or ", updateDownloader), "Updates the downloader using the command specified in the settings"
            (updateAudioFormatPrefix,
                  $"Followed by a supported audio format (e.g., %s{updateAudioFormatPrefix}m4a), changes the audio format for the current session only")
            (updateAudioQualityPrefix,
                  $"Followed by a supported audio quality (e.g., %s{updateAudioQualityPrefix}0), changes the audio quality for the current session only")
            String.Join(" or ", quitCommands), "Quit the application"
            helpCommand, "See this help message"
        ]
        |> Map.ofList
