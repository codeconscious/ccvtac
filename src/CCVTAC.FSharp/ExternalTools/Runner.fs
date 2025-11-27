namespace CCVTAC.Console.ExternalTools

open System
open CCVTAC.Console
open Startwatch.Library
open System.Diagnostics

module Runner =
    [<Literal>]
    let private AuthenticSuccessExitCode = 0

    let private isSuccessExitCode (otherSuccessExitCodes: int list) (exitCode: int) =
        List.contains exitCode (List.append otherSuccessExitCodes [AuthenticSuccessExitCode])

    /// Calls an external application.
    /// <param name="settings">Tool settings for execution</param>
    /// <param name="otherSuccessExitCodes">Additional exit codes, other than 0, that can be treated as non-failures</param>
    /// <param name="printer">Printer for logging</param>
    /// <returns>A `Result` containing the exit code, if successful, or else an error message</returns>
    let internal run
        (settings: ToolSettings)
        (otherSuccessExitCodes: int list)
        (printer: Printer)
        : Result<int * string option, string> =

        let watch = Watch()

        // Log start of execution
        printer.Info($"Running {settings.CommandWithArgs}...")

        // Split command and arguments
        let splitCommandWithArgs =
            settings.CommandWithArgs.Split([|' '|], 2)

        // Prepare process start info
        let processStartInfo = ProcessStartInfo splitCommandWithArgs[0]
        // processStartInfo.FileName <- splitCommandWithArgs[0]
        processStartInfo.Arguments <- if splitCommandWithArgs.Length > 1 then splitCommandWithArgs[1] else String.Empty
        processStartInfo.UseShellExecute <- false
        processStartInfo.RedirectStandardOutput <- false
        processStartInfo.RedirectStandardError <- true
        processStartInfo.CreateNoWindow <- true
        processStartInfo.WorkingDirectory <- settings.WorkingDirectory

        match Process.Start processStartInfo with
        | null ->
            Error $"Could not locate {splitCommandWithArgs[0]}."
        | process' ->
            let error = process'.StandardError.ReadToEnd()

            process'.WaitForExit()

            printer.Info($"{splitCommandWithArgs[0]} finished in {watch.ElapsedFriendly}.")

            let trimmedErrors = if hasNonWhitespaceText error then Some (trimTerminalLineBreak error) else None

            if isSuccessExitCode otherSuccessExitCodes process'.ExitCode then
                Ok (process'.ExitCode, trimmedErrors)
            else
                Error $"{splitCommandWithArgs[0]} exited with code {process'.ExitCode}: {trimmedErrors}."
