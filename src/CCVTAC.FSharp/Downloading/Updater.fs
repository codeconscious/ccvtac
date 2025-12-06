namespace CCVTAC.Console.Downloading

open CCVTAC.Console.ExternalTools
open CCVTAC.Console
open CCVTAC.Console.Settings.Settings

module Updater =

    let run userSettings (printer: Printer) : Result<unit,string> =
        if String.hasNoText userSettings.DownloaderUpdateCommand then
            printer.Info("No downloader update command provided, so will skip.")
            Ok()
        else
            let toolSettings = ToolSettings.create userSettings.DownloaderUpdateCommand userSettings.WorkingDirectory

            match Runner.run toolSettings [] printer with
            | Ok result ->
                if result.ExitCode <> 0 then
                    printer.Warning("Tool updated with minor issues.")

                    match result.Error with
                    | Some w -> printer.Warning w
                    | None -> ()

                Ok()

            | Error err ->
                printer.Error $"Failure updating: {err}"
                Error err
