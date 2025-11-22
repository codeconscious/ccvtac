namespace CCVTAC.Console.ExternalTools

open System.Diagnostics

module Runner =
    /// Authentic success exit code
    [<Literal>]
    let private AuthenticSuccessExitCode = 0

    /// Determines if the exit code is considered successful
    let private isSuccessExitCode (otherSuccessExitCodes: int[]) (exitCode: int) =
        Array.contains exitCode (Array.append otherSuccessExitCodes [|AuthenticSuccessExitCode|])

    /// Calls an external application.
    /// <param name="settings">Tool settings for execution</param>
    /// <param name="otherSuccessExitCodes">Additional exit codes, other than 0, that can be treated as non-failures</param>
    /// <param name="printer">Printer for logging</param>
    /// <returns>A `Result` containing the exit code, if successful, or else an error message</returns>
    let internal run
        (settings: ToolSettings)
        (otherSuccessExitCodes: int[])
        (printer: Printer) =

        let watch = Watch()

        // Log start of execution
        printer.Info($"Running {settings.CommandWithArgs}...")

        // Split command and arguments
        let splitCommandWithArgs =
            settings.CommandWithArgs.Split([|' '|], 2)

        // Prepare process start info
        let processStartInfo = ProcessStartInfo(
            FileName = splitCommandWithArgs.[0],
            Arguments = if splitCommandWithArgs.Length > 1 then splitCommandWithArgs.[1] else "",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = settings.WorkingDirectory
        )

        // Start the process
        match Process.Start(processStartInfo) with
        | null ->
            // Process failed to start
            Error $"Could not locate {splitCommandWithArgs.[0]}."
        | process ->
            // Read errors before waiting for exit
            let errors = process.StandardError.ReadToEnd()

            // Wait for process to complete
            process.WaitForExit()

            // Log completion time
            printer.Info($"{splitCommandWithArgs.[0]} finished in {watch.ElapsedFriendly}.")

            // Trim terminal line break from errors
            let trimmedErrors = errors.TrimTerminalLineBreak()

            // Determine result based on exit code
            if isSuccessExitCode otherSuccessExitCodes process.ExitCode then
                // Successful execution (with potential warnings)
                Ok (process.ExitCode, trimmedErrors)
            else
                // Failed execution
                Error $"{splitCommandWithArgs.[0]} exited with code {process.ExitCode}: {trimmedErrors}."
