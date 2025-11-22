namespace CCVTAC.Console

open System
open System.Linq
open Spectre.Console
open CCVTAC.Console.IoUtilities
open CCVTAC.Console.Settings

module Program =

    let private helpFlags = [| "-h"; "--help" |]
    let private settingsFileFlags = [| "-s"; "--settings" |]
    let private defaultSettingsFileName = "settings.json"

    [<EntryPoint>]
    let main (args: string[]) : int =
        let printer = Printer(showDebug = true)

        if args.Length > 0 && ExtensionMethods.CaseInsensitiveContains(helpFlags, args.[0]) then
            Help.Print(printer)
            0
        else
            let maybeSettingsPath =
                if args.Length >= 2 && ExtensionMethods.CaseInsensitiveContains(settingsFileFlags, args.[0]) then
                    args.[1] // expected to be a settings file path
                else
                    defaultSettingsFileName

            match SettingsAdapter.ProcessSettings(maybeSettingsPath, printer) with
            | Error errs ->
                // Errors prints the messages and exits
                printer.Errors(errs.Select(fun e -> e.Message).ToList())
                1
            | Ok None ->
                // A new settings file was created; nothing more to do
                0
            | Ok (Some settings) ->
                SettingsAdapter.PrintSummary(settings, printer, header = "Settings loaded OK.")
                printer.ShowDebug(not settings.QuietMode)

                // Catch Ctrl-C (SIGINT)
                Console.CancelKeyPress.Add(fun args ->
                    printer.Warning("\nQuitting at user's request.")

                    match Directories.WarnIfAnyFiles(settings.WorkingDirectory, 10) with
                    | Ok () -> ()
                    | Error warnResult ->
                        printer.FirstError(warnResult)
                        match Directories.AskToDeleteAllFiles(settings.WorkingDirectory, printer) with
                        | Ok deletedCount -> printer.Info(sprintf "%d file(s) deleted." deletedCount)
                        | Error delErr -> printer.FirstError(delErr)
                    // Do not set args.Cancel here; let default behavior terminate the process
                )

                // Top-level try to catch unexpected exceptions and report them
                try
                    Orchestrator.Start(settings, printer)
                    0
                with ex ->
                    printer.Critical(sprintf "Fatal error: %s" ex.Message)
                    AnsiConsole.WriteException(ex)
                    printer.Info(
                        "Please help improve this tool by reporting this error and any relevant URLs at https://github.com/codeconscious/ccvtac/issues."
                    )
                    1
