namespace CCVTAC.Console.ExternalTools

open System.Diagnostics

type ExternalTool = private {
    Name: string
    Url: string
    Purpose: string
}

module ExternalTool =
    /// Creates a new ExternalTool instance
    /// <param name="name">The name of the program. This should be the exact text used to call it
    /// on the command line, excluding any arguments.</param>
    /// <param name="url">The URL of the program, from which users should install it if needed.</param>
    /// <param name="purpose">A brief summary of the purpose of the program within the context of this program.
    /// Should be phrased as a noun (e.g., "image processing" or "audio normalization").</param>
    let create (name: string) (url: string) (purpose: string) =
        { Name = name.Trim()
          Url = url.Trim()
          Purpose = purpose.Trim() }

    /// Attempts a dry run of the program to determine if it is installed and available on this system.
    /// <returns>A Result indicating whether the program is available or not.</returns>
    let programExists name =
        let processStartInfo = ProcessStartInfo(
            FileName = name,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true)

        try
            match Process.Start processStartInfo with
            | Null ->
                Error $"The program \"{name}\" was not found. (The process was null.)"
            | NonNull process' ->
                process'.WaitForExit()
                Ok()
        with
        | exn -> Error $"The program \"{name}\" was not found or could not be run: {exn.Message}."
