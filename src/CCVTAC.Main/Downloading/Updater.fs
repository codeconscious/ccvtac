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

            let executionResult = Runner.runTool toolSettings [] printer

            match executionResult with
            | Ok details ->
                if details.ExitCode <> successExitCode then
                    match details.Error with
                    | Some errMsg -> $"Update completed with minor issues: {errMsg}"
                    | None        ->  "Update completed with minor unspecified issues."
                    |> printer.Warning
                printer.EmptyLines 1uy
            | Error msg ->
                printer.Error($"Failure updating: {msg}", ?appendLines = Some 1uy)
