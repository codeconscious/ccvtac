namespace CCVTAC.Console.Downloading

open CCVTAC.Console.ExternalTools
open CCVTAC.Console
open CCVTAC.Console.Settings.Settings

/// Manages downloader updates
module Updater =

    type private Urls = {
        Primary: string
        Supplementary: string option
    }

    /// Completes the actual download process.
    /// <returns>A `Result` that, if successful, contains the name of the successfully downloaded format.</returns>
    let internal run (settings: UserSettings) (printer: Printer) =
        // Check if update command is provided
        if String.hasNoText settings.DownloaderUpdateCommand then
            printer.Info("No downloader update command provided, so will skip.")
            Ok()
        else
            let settings = ToolSettings.create settings.DownloaderUpdateCommand settings.WorkingDirectory

            match Runner.run settings [] printer with
            | Ok (exitCode, warnings) ->
                if exitCode <> 0 then
                    printer.Warning("Update completed with minor issues.")

                    match warnings with
                    | Some w -> printer.Warning w
                    | None -> ()

                Ok()

            | Error error ->
                printer.Error $"Failure updating: {error}"
                Error error


