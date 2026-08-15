namespace CCVTAC.Main.Downloading

open CCVTAC.Main
open CCVTAC.Main.ExternalTools
open CCVTAC.Main.Settings.Settings
open CCFSharpUtils.Text

module Updater =

    let successExitCode = 0

    let run userSettings (printer: Printer) : unit =
        if String.hasNoText userSettings.DownloaderUpdateCommand then
            printer.Info "No downloader update command provided, so will skip."
        else
            let toolSettings = ToolSettings.create userSettings.DownloaderUpdateCommand
                                                   userSettings.WorkingDirectory

            match Runner.runTool toolSettings [] printer with
            | Ok result ->
                if result.ExitCode <> successExitCode then
                    match result.Error with
                    | Some w -> $"Update completed with minor issues: {w}"
                    | None   ->  "Update completed with minor unspecified issues."
                    |> printer.Warning
                printer.EmptyLines 1uy
            | Error err ->
                printer.Error($"Failure updating: {err}", ?appendLines = Some 1uy)
