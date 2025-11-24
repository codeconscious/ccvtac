namespace CCVTAC.Console

open System
open System.Linq
open Spectre.Console
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings
open ExtensionMethods
open CCVTAC.Console
open CCVTAC.Console.Settings
open CCVTAC.Console.Settings.Settings.IO
open CCVTAC.Console.Settings.Settings.LiveUpdating
open CCVTAC.Console.Settings.Settings.Validation

module Program =

    let private helpFlags = [| "-h"; "--help" |]
    let private settingsFileFlags = [| "-s"; "--settings" |]
    let private defaultSettingsFileName = "settings.json"

    [<EntryPoint>]
    let main (args: string[]) : int =
        let printer = Printer(showDebug = true)

        if args.Length > 0 && caseInsensitiveContains helpFlags args[0] then
            Help.Print(printer)
            0
        else
            let maybeSettingsPath =
                if args.Length >= 2 && caseInsensitiveContains settingsFileFlags args[0] then
                    args.[1] // expected to be a settings file path
                else
                    defaultSettingsFileName

            // match SettingsAdapter.ProcessSettings(maybeSettingsPath, printer) with
            let readResult = Settings.IO.read (FilePath maybeSettingsPath)
            match readResult with
            | Error e ->
                printer.Error(e)
                1
            // | Ok None ->
            //     // A new settings file was created; nothing more to do
            //     0
            | Ok settings ->
                Settings.PrintSummary settings printer (Some "Settings loaded OK.")
                printer.ShowDebug(not settings.QuietMode)

                // Catch Ctrl-C (SIGINT)
                Console.CancelKeyPress.Add(fun args ->
                    printer.Warning("\nQuitting at user's request.")

                    match Directories.warnIfAnyFiles settings.WorkingDirectory 10 with
                    | Ok () -> ()
                    | Error warnResult ->
                        printer.FirstError(warnResult)
                        match Directories.askToDeleteAllFiles settings.WorkingDirectory printer with
                        | Ok deletedCount -> printer.Info $"%d{deletedCount} file(s) deleted."
                        | Error delErr -> printer.FirstError(delErr)
                    // Do not set args.Cancel here; let default behavior terminate the process
                )

                // Top-level try to catch unexpected exceptions and report them
                try
                    Orchestrator.start settings printer
                    0
                with ex ->
                    printer.Critical $"Fatal error: %s{ex.Message}"
                    AnsiConsole.WriteException(ex)
                    printer.Info(
                        "Please help improve this tool by reporting this error and any relevant URLs at https://github.com/codeconscious/ccvtac/issues."
                    )
                    1
