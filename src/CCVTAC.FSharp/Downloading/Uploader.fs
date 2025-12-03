namespace CCVTAC.Console

open CCVTAC.Console.ExternalTools
open CCVTAC.Console.Settings.Settings

module Updater =

    type private Urls =
        { Primary: string
          Supplementary: string option }

    let internal run (settings: UserSettings) (printer: Printer) : Result<unit,string> =
        if String.hasNoText settings.DownloaderUpdateCommand then
            printer.Info("No downloader update command provided, so will skip.")
            Ok()
        else
            let args = ToolSettings.create settings.DownloaderUpdateCommand settings.WorkingDirectory

            match Runner.run args [] printer with
            | Ok (exitCode, warnings) ->
                if exitCode <> 0 then
                    printer.Warning "Update completed with minor issues."
                    match warnings with
                    | Some w -> printer.Warning w
                    | None -> ()
                Ok()
            | Error error ->
                printer.Error($"Failure updating: %s{error}")
                Error error
