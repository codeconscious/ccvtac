namespace CCVTAC.Console

open System
open Spectre.Console
open CCVTAC.Console
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings

module Program =

    let private helpFlags = [| "-h"; "--help" |]
    let private settingsFileFlags = [| "-s"; "--settings" |]
    let private defaultSettingsFileName = "settings.json"

    type ExitCodes =
        | Success = 0
        | ArgError = 1
        | OperationError = 2

    [<EntryPoint>]
    let main (args: string array) : int =
        let printer = Printer(showDebug = true)

        if args.Length > 0 && caseInsensitiveContains helpFlags args[0] then
            Help.Print printer
            int ExitCodes.Success
        else
            let maybeSettingsPath =
                if args.Length >= 2 && caseInsensitiveContains settingsFileFlags args[0] then
                    args[1] // Expected to be a settings file path
                else
                    defaultSettingsFileName

            // match SettingsAdapter.ProcessSettings(maybeSettingsPath, printer) with
            let readResult = Settings.IO.read (FilePath maybeSettingsPath)
            match readResult with
            | Error e ->
                printer.Error e
                int ExitCodes.ArgError
            // | Ok None ->
            //     // A new settings file was created; nothing more to do
            //     0
            | Ok settings ->
                Settings.printSummary settings printer (Some "Settings loaded OK.")
                printer.ShowDebug(not settings.QuietMode)

                // Catch Ctrl-C (SIGINT)
                Console.CancelKeyPress.Add(fun _ ->
                    printer.Warning("\nQuitting at user's request.")

                    match Directories.warnIfAnyFiles settings.WorkingDirectory 10 with
                    | Ok () -> ()
                    | Error warnResult ->
                        printer.FirstError warnResult
                        match Directories.askToDeleteAllFiles settings.WorkingDirectory printer with
                        | Ok deletedCount -> printer.Info $"%d{deletedCount} file(s) deleted."
                        | Error delErr -> printer.FirstError delErr
                )

                try
                    Orchestrator.start settings printer
                    int ExitCodes.Success
                with ex ->
                    printer.Critical $"Fatal error: %s{ex.Message}"
                    AnsiConsole.WriteException ex
                    printer.Info(
                        "Please help improve this tool by reporting this error and any relevant URLs at https://github.com/codeconscious/ccvtac/issues."
                    )
                    int ExitCodes.OperationError
