namespace CCVTAC.Main

open CCVTAC.Main.IoUtilities
open CCVTAC.Main.Orchestrator
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

    type ExitCodes =
        | Success = 0
        | ArgError = 1
        | OperationError = 2

    type private Command =
        | ShowHelp
        | Run of settingsPath: FileInfo
        | Invalid of reason: string

    let private parse args =
        match args with
        | [| flag; path |] when Array.containsIgnoreCase flag settingsFileFlags ->
            Run (FileInfo path)
        | [| flag; _ |] ->
            Invalid $"Unrecognized flag '{flag}'."
        | _ ->
            ShowHelp

    let private registerCancelHandler (printer: Printer) workingDirectory =
        Console.CancelKeyPress.Add(fun _ ->
            printer.Warning $"{String.nl}Quitting at user's request."

            match Directories.warnIfAnyFiles 10 workingDirectory with
            | Ok () -> ()
            | Error warnResult ->
                printer.Error warnResult
                match Directories.askToDeleteAllFiles printer workingDirectory with
                | Error err  -> printer.Error err
                | Ok results -> Directories.printDeletionResults printer results)

    let private run (printer: Printer) (settingsFile: FileInfo) : int =
        if not settingsFile.Exists then
            match writeDefaultFile settingsFile with
            | Ok msg ->
                printer.Info msg
                int ExitCodes.Success
            | Error err ->
                printer.Error err
                int ExitCodes.OperationError
        else
            match read settingsFile with
            | Error err ->
                printer.Error err
                int ExitCodes.ArgError
            | Ok settings ->
                printer.Info "Settings loaded OK."
                printer.PrintTable (toTable settings)
                printer.ShowDebug(not settings.QuietMode)

                registerCancelHandler printer settings.WorkingDirectory

                try
                    match Orchestrator.start printer settings with
                    | QuitDueToErrors -> int ExitCodes.OperationError
                    | _               -> int ExitCodes.Success
                with exn ->
                    printer.Critical $"Fatal error: {exn.Message}"
                    AnsiConsole.WriteException exn
                    printer.Info "Please help improve this tool by reporting this error and any relevant data (URLs, etc.) at https://github.com/codeconscious/ccvtac/issues."
                    int ExitCodes.OperationError

    [<EntryPoint>]
    let main args : int =
        let printer = Printer(showDebug = true)

        match parse args with
        | ShowHelp ->
            printer.Info Help.helpText
            int ExitCodes.Success
        | Invalid reason ->
            printer.Error reason
            printer.Info Help.helpText
            int ExitCodes.ArgError
        | Run settingsPath ->
            run printer settingsPath
