namespace CCVTAC.Console

open System.IO
open CCVTAC.Console
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings
open System
open Spectre.Console

module Program =

    let private helpFlags = [| "-h"; "--help" |]
    let private settingsFileFlags = [| "-s"; "--settings" |]
    let private defaultSettingsFileName = "settings.json"

    type ExitCodes =
        | Success = 0
        | ArgError = 1
        | OperationError = 2

    [<EntryPoint>]
    let main args : int =
        let printer = Printer(showDebug = true)

        if Array.isNotEmpty args && Array.caseInsensitiveContains args[0] helpFlags then
            printer.Info Help.helpText
            int ExitCodes.Success
        else
            let settingsPath =
                if Array.hasMultiple args && Array.caseInsensitiveContains args[0] settingsFileFlags then
                    args[1] // Expected to be a settings file path.
                else
                    defaultSettingsFileName
                |> FileInfo

            if not settingsPath.Exists then
                match Settings.IO.writeDefaultFile settingsPath with
                | Ok msg ->
                    printer.Info msg
                    int ExitCodes.Success
                | Error err ->
                    printer.Error err
                    int ExitCodes.OperationError
            else
                let readResult = Settings.IO.read settingsPath
                match readResult with
                | Error e ->
                    printer.Error e
                    int ExitCodes.ArgError
                | Ok settings ->
                    Settings.printSummary settings printer (Some "Settings loaded OK.")
                    printer.ShowDebug(not settings.QuietMode)

                    // Catch Ctrl-C (SIGINT)
                    Console.CancelKeyPress.Add(fun _ ->
                        printer.Warning($"{String.newLine}Quitting at user's request.")

                        match Directories.warnIfAnyFiles 10 settings.WorkingDirectory with
                        | Ok () -> ()
                        | Error warnResult ->
                            printer.Error warnResult
                            match Directories.askToDeleteAllFiles settings.WorkingDirectory printer with
                            | Ok deletedCount -> printer.Info $"%d{deletedCount} file(s) deleted."
                            | Error delErr -> printer.Error delErr
                    )

                    try
                        Orchestrator.start settings printer
                        int ExitCodes.Success
                    with ex ->
                        printer.Critical $"Fatal error: %s{ex.Message}"
                        AnsiConsole.WriteException ex
                        printer.Info "Please help improve this tool by reporting this error and any relevant URLs at https://github.com/codeconscious/ccvtac/issues."
                        int ExitCodes.OperationError
