namespace CCVTAC.Console.Downloading

open CCVTAC.Console.ExternalTools
open CCVTAC.FSharp.Settings

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
            // Prepare tool settings
            let args = ToolSettings(
                settings.DownloaderUpdateCommand,
                settings.WorkingDirectory
            )

            // Run the update process
            match Runner.Run(args, [||], printer) with
            | Ok (exitCode, warnings) ->
                // Handle successful run with potential warnings
                if exitCode <> 0 then
                    printer.Warning("Update completed with minor issues.")

                    if not (System.String.IsNullOrEmpty(warnings)) then
                        printer.Warning(warnings)

                Ok()

            | Error errors ->
                // Handle errors
                printer.Error("Failure updating...")

                // Print and process errors
                errors
                |> Array.iter (fun e -> printer.Error(e.Message))

                // Return failure result if errors exist
                if errors.Length > 0 then
                    Error (System.String.Join(" / ", errors |> Array.map (fun e -> e.Message)))
                else
                    Ok()
 CCVTAC.FSharp.Downloading.Downloader

