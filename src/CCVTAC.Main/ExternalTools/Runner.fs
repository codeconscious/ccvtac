namespace CCVTAC.Main.ExternalTools

open CCVTAC.Main
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
    let runTool toolSettings otherSuccessExitCodes (printer: Printer)
        : Result<ToolResult, string> =

        let watch = Watch()
        printer.Info $"Running {toolSettings.CommandWithArgs}..."

        let command =
            toolSettings.CommandWithArgs.Split([|' '|], 2)
            |> fun arr -> {| Tool = arr[0]
                             Args = if Array.hasMultiple arr then arr[1] else String.Empty |}

        let processStartInfo = ProcessStartInfo command.Tool
        processStartInfo.Arguments <- command.Args
        processStartInfo.RedirectStandardOutput <- false
        processStartInfo.RedirectStandardError <- true
        processStartInfo.CreateNoWindow <- true
        processStartInfo.WorkingDirectory <- toolSettings.WorkingDirectory

        match Process.Start processStartInfo with
        | Null ->
            Error $"Could not locate or start {command.Tool}."
        | NonNull process' ->
            let error = process'.StandardError.ReadToEnd()

            process'.WaitForExit()
            printer.Info $"{command.Tool} finished in {watch.ElapsedFriendly}."

            let trimmedError = if String.hasText error
                               then Some (String.trimTerminalLineBreak error)
                               else None

            if isSuccessExitCode otherSuccessExitCodes process'.ExitCode
            then Ok { ExitCode = process'.ExitCode; Error = trimmedError }
            else Error $"{command.Tool} exited with code {process'.ExitCode}: {trimmedError}."
