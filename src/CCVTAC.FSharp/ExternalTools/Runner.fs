namespace CCVTAC.Console.ExternalTools

open CCVTAC.Console
open Startwatch.Library
open System
open System.Diagnostics

module Runner =

    type ToolResult = { ExitCode: int; Error: string option }

    [<Literal>]
    let private authenticSuccessExitCode = 0

    let private isSuccessExitCode (otherSuccessExitCodes: int list) (exitCode: int) =
        List.contains exitCode (authenticSuccessExitCode :: otherSuccessExitCodes)

    /// Calls an external application.
    /// <param name="settings">Tool settings for execution</param>
    /// <param name="otherSuccessExitCodes">Additional exit codes, other than 0, that can be treated as non-failures</param>
    /// <param name="printer">Printer for logging</param>
    /// <returns>A Result instance containing the exit code and any warnings or else an error message.</returns>
    let internal run toolSettings (otherSuccessExitCodes: int list) (printer: Printer)
        : Result<ToolResult, string> =

        let watch = Watch()
        printer.Info $"Running {toolSettings.CommandWithArgs}..."

        let splitCommandWithArgs = toolSettings.CommandWithArgs.Split([|' '|], 2)

        let processStartInfo = ProcessStartInfo splitCommandWithArgs[0]
        processStartInfo.Arguments <- if splitCommandWithArgs.Length > 1
                                      then splitCommandWithArgs[1]
                                      else String.Empty
        processStartInfo.UseShellExecute <- false
        processStartInfo.RedirectStandardOutput <- false
        processStartInfo.RedirectStandardError <- true
        processStartInfo.CreateNoWindow <- true
        processStartInfo.WorkingDirectory <- toolSettings.WorkingDirectory

        match Process.Start processStartInfo with
        | null ->
            Error $"Could not locate {splitCommandWithArgs[0]}."
        | process' ->
            let error = process'.StandardError.ReadToEnd()

            process'.WaitForExit()
            printer.Info $"{splitCommandWithArgs[0]} finished in {watch.ElapsedFriendly}."

            let trimmedErrors = if String.hasText error
                                then Some (String.trimTerminalLineBreak error)
                                else None

            if isSuccessExitCode otherSuccessExitCodes process'.ExitCode
            then Ok { ExitCode = process'.ExitCode; Error = trimmedErrors }
            else Error $"{splitCommandWithArgs[0]} exited with code {process'.ExitCode}: {trimmedErrors}."
