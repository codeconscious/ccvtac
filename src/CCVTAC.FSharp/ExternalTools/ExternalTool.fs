namespace CCVTAC.Console.ExternalTools

open System.Diagnostics

type ExternalTool = {
    /// The name of the program. This should be the exact text used to call it
    /// on the command line, excluding any arguments.
    Name: string

    /// The URL of the program, from which users should install it if needed.
    Url: string

    /// A brief summary of the purpose of the program within the context of this program.
    /// Should be phrased as a noun (e.g., "image processing" or "audio normalization").
    Purpose: string
} with
    /// Creates a new ExternalTool instance
    /// <param name="name">The name of the program. This should be the exact text used to call it
    /// on the command line, excluding any arguments.</param>
    /// <param name="url">The URL of the program, from which users should install it if needed.</param>
    /// <param name="purpose">A brief summary of the purpose of the program within the context of this program.
    /// Should be phrased as a noun (e.g., "image processing" or "audio normalization").</param>
    static member Create(name: string, url: string, purpose: string) =
        {
            Name = name.Trim()
            Url = url.Trim()
            Purpose = purpose.Trim()
        }

    /// Attempts a dry run of the program to determine if it is installed and available on this system.
    /// <returns>A Result indicating whether the program is available or not.</returns>
    member this.ProgramExists() =
        let processStartInfo = ProcessStartInfo(
            FileName = this.Name,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        )

        try
            use process' = Process.Start(processStartInfo)

            match process' with
            | null ->
                Error $"The program \"{this.Name}\" was not found. (The process was null.)"
            | process'' ->
                process''.WaitForExit()
                Ok()
        with
        | _ ->
            Error $"The program \"{this.Name}\" was not found."
