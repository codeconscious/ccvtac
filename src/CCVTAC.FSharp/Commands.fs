namespace CCVTAC.Console

open System

module Commands =

    let prefix = '\\'

    let private toCommand text : string =
        if String.hasNoText text then
            raise (ArgumentException("Commands cannot be null or white space.", "text"))
        if text.Contains ' ' then
            raise (ArgumentException("Commands cannot contain white space.", "text"))
        $"%c{prefix}%s{text}"

    let quitCommands = [| toCommand "quit"; toCommand "q"; toCommand "exit" |]
    let helpCommand: string = toCommand "help"
    let settingsSummary = [| toCommand "settings" |]
    let history = [| toCommand "history" |]
    let updateDownloader = [| toCommand "update-downloader"; toCommand "update-dl" |]
    let splitChapterToggles = [| toCommand "split"; toCommand "toggle-split" |]
    let embedImagesToggles = [| toCommand "images"; toCommand "toggle-images" |]
    let quietModeToggles = [| toCommand "quiet"; toCommand "toggle-quiet" |]
    let updateAudioFormatPrefix: string = toCommand "format-"
    let updateAudioQualityPrefix: string = toCommand "quality-"

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
