namespace CCVTAC.Main

open CCVTAC.Main
open CCVTAC.Main.IoUtilities
open CCVTAC.Main.Settings
open CCVTAC.Main.Settings.Settings
open Settings.IO
open CCFSharpUtils.Collections
open CCFSharpUtils.Text
open Spectre.Console
open System
open System.IO

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

        match args with
        | [||] ->
            printer.Info Help.helpText
            int ExitCodes.Success
        | [| arg |] when helpFlags |> Array.containsIgnoreCase arg ->
            printer.Info Help.helpText
            int ExitCodes.Success
        | [| flagArg; settingsFileArg |] ->
            let settingsPath =
                FileInfo <|
                    if settingsFileFlags |> Array.containsIgnoreCase flagArg
                    then settingsFileArg
                    else defaultSettingsFileName

            if not settingsPath.Exists then
                match writeDefaultFile settingsPath with
                | Ok msg ->
                    printer.Info msg
                    int ExitCodes.Success
                | Error err ->
                    printer.Error err
                    int ExitCodes.OperationError
            else
                match read settingsPath with
                | Error err ->
                    printer.Error err
                    int ExitCodes.ArgError
                | Ok settings ->
                    printer.Info "Settings loaded OK."
                    settings |> toTable |> printer.PrintTable

                    printer.ShowDebug(not settings.QuietMode)

                    // Catch Ctrl-C (SIGINT)
                    Console.CancelKeyPress.Add(fun _ ->
                        printer.Warning($"{String.nl}Quitting at user's request.")

                        match Directories.warnIfAnyFiles 10 settings.WorkingDirectory with
                        | Ok () -> ()
                        | Error warnResult ->
                            printer.Error warnResult
                            match Directories.askToDeleteAllFiles printer settings.WorkingDirectory with
                            | Error err  -> printer.Error err
                            | Ok results -> Directories.printDeletionResults printer results)
                    try
                        Orchestrator.start printer settings
                        int ExitCodes.Success
                    with exn ->
                        printer.Critical $"Fatal error: %s{exn.Message}"
                        AnsiConsole.WriteException exn
                        printer.Info "Please help improve this tool by reporting this error and any relevant URLs at https://github.com/codeconscious/ccvtac/issues."
                        int ExitCodes.OperationError
        | _ ->
            printer.Info Help.helpText
            int ExitCodes.Success
