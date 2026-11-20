namespace CCVTAC.Main.Downloading

open CCVTAC.Main
open CCVTAC.Main.ExternalTools
open CCVTAC.Main.Settings.Settings
open CCFSharpUtils.Text

module Updater =

    let successExitCode = 0

    let run (printer: Printer) userSettings : unit =
        if String.hasNoText userSettings.DownloaderUpdateCommand then
            printer.Info "No downloader update command provided, so will skip updating."
        else
            let toolSettings = ToolSettings.create userSettings.DownloaderUpdateCommand
                                                   userSettings.WorkingDirectory

            match Runner.runTool printer toolSettings [] with
            | Ok details ->
                if details.ExitCode <> successExitCode then
                    let msg =
                        match details.Error with
                        | Some errMsg -> $"Update completed with minor issues: {errMsg}"
                        | None        ->  "Update completed with minor unspecified issues."
                    printer.Warning msg
            | Error msg ->
                printer.Error $"Failure updating: {msg}"
