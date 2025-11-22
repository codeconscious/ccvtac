module CCVTAC.FSharp.Downloading.Uploader

open CCVTAC.Console.ExternalTools
open CCVTAC.FSharp.Settings

module Updater =
    /// Represents download URLs
    type private Urls = {
        Primary: string
        Supplementary: string option
    }

    /// Completes the actual download process.
    /// <returns>A `Result` that, if successful, contains the name of the successfully downloaded format.</returns>
    let internal run (settings: UserSettings) (printer: Printer) =
        // Early return if no update command
        if System.String.IsNullOrWhiteSpace(settings.DownloaderUpdateCommand) then
            printer.Info("No downloader update command provided, so will skip.")
            Ok()
        else
            // Prepare tool settings
            let args = ToolSettings(
                settings.DownloaderUpdateCommand,
                settings.WorkingDirectory
            )

            // Run the update process
            match Runner.Run(args, [||], printer) with
            | Ok (exitCode, warnings) ->
                // Handle non-zero exit code with potential warnings
                if exitCode <> 0 then
                    printer.Warning("Update completed with minor issues.")

                    if not (System.String.IsNullOrEmpty(warnings)) then
                        printer.Warning(warnings)

                Ok()

            | Error errors ->
                // Handle and log errors
                printer.Error("Failure updating...")

                // Collect error messages
                let errorMessages =
                    errors
                    |> Array.map (fun e -> e.Message)

                // Print individual error messages
                errorMessages
                |> Array.iter printer.Error

                // Return result based on error messages
                if errorMessages.Length > 0 then
                    Error (System.String.Join(" / ", errorMessages))
                else
                    Ok()
