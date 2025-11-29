namespace CCVTAC.Console

open System
open System.Collections.Generic

module internal Commands =

    let Prefix : char = '\\'

    let private MakeCommand (text: string) : string =
        if hasNoText text then
            raise (ArgumentException("The text cannot be null or white space.", "text"))
        if text.Contains ' ' then
            raise (ArgumentException("The text should not contain any white space.", "text"))
        $"%c{Prefix}%s{text}"

    let QuitCommands : string[] =
        [| MakeCommand "quit"; MakeCommand "q"; MakeCommand "exit" |]

    let HelpCommand : string = MakeCommand "help"

    let SettingsSummary : string[] = [| MakeCommand "settings" |]

    let History : string[] = [| MakeCommand "history" |]

    let UpdateDownloader : string[] =
        [| MakeCommand "update-downloader"; MakeCommand "update-dl" |]

    let SplitChapterToggles : string[] = [| MakeCommand "split"; MakeCommand "toggle-split" |]

    let EmbedImagesToggles : string[] = [| MakeCommand "images"; MakeCommand "toggle-images" |]

    let QuietModeToggles : string[] = [| MakeCommand "quiet"; MakeCommand "toggle-quiet" |]

    let UpdateAudioFormatPrefix : string = MakeCommand "format-"

    let UpdateAudioQualityPrefix : string = MakeCommand "quality-"

    let Summary : Dictionary<string, string> =
        let d = Dictionary<string, string>()
        d.Add(String.Join(" or ", History), "See the most recently entered URLs")
        d.Add(String.Join(" or ", SplitChapterToggles), "Toggles chapter splitting for the current session only")
        d.Add(String.Join(" or ", EmbedImagesToggles), "Toggles image embedding for the current session only")
        d.Add(String.Join(" or ", QuietModeToggles), "Toggles quiet mode for the current session only")
        d.Add(String.Join(" or ", UpdateDownloader), "Updates the downloader using the command specified in the settings")
        d.Add(UpdateAudioFormatPrefix,
              sprintf "Followed by a supported audio format (e.g., %sm4a), changes the audio format for the current session only" UpdateAudioFormatPrefix)
        d.Add(UpdateAudioQualityPrefix,
              sprintf "Followed by a supported audio quality (e.g., %s0), changes the audio quality for the current session only" UpdateAudioQualityPrefix)
        d.Add(String.Join(" or ", QuitCommands), "Quit the application")
        d.Add(HelpCommand, "See this help message")
        d
