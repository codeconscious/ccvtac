namespace CCVTAC.Console.Downloading

open CCVTAC.Console.ExternalTools
open CCVTAC.Console
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings

/// Manages downloader updates
module Updater =
    /// Represents download URLs
    type private Urls = {
        Primary: string
        Supplementary: string option
    }

    /// Completes the actual download process.
    /// <returns>A `Result` that, if successful, contains the name of the successfully downloaded format.</returns>
    let internal run (settings: UserSettings) (printer: Printer) =
        // Check if update command is provided
        if System.String.IsNullOrWhiteSpace(settings.DownloaderUpdateCommand) then
            printer.Info("No downloader update command provided, so will skip.")
            Ok()
        else
            let args : ToolSettings = {
                CommandWithArgs = settings.DownloaderUpdateCommand
                WorkingDirectory = settings.WorkingDirectory
            }

            // Run the update process
            match Runner.run args [] printer with
            | Ok (exitCode, warnings) ->
                // Handle successful run with potential warnings
                if exitCode <> 0 then
                    printer.Warning("Update completed with minor issues.")

                    match warnings with
                    | Some w -> printer.Warning w
                    | None -> ()

                Ok()

            | Error error ->
                printer.Error($"Failure updating: {error}")
                Error error


