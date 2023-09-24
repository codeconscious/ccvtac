using System.Diagnostics;

namespace CCVTAC.Console.ExternalUtilties;

internal static class Caller
{
    internal static Result<int> Run(UtilitySettings toolSettings, Printer printer)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        printer.Print($"Starting {toolSettings.Program.Name} for {toolSettings.Program.Purpose}...");
        printer.Print($"Running command: {toolSettings.Program.Name} {toolSettings.Args}");

        var processStartInfo = new ProcessStartInfo()
        {
            FileName = toolSettings.Program.Name,
            Arguments = toolSettings.Args,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true,
            WorkingDirectory = toolSettings.WorkingDirectory
        };

        using var process = Process.Start(processStartInfo);
        if (process is null)
        {
            return Result.Fail($"Could not start {toolSettings.Program.Name}. " +
                $"Please install it from {toolSettings.Program.Url}.");
        }
        process.WaitForExit();

        printer.Print($"Done with {toolSettings.Program.Purpose} in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return process.ExitCode == 0
            ? Result.Ok(process.ExitCode)
            : Result.Fail($"Full or partial download error ({toolSettings.Program.Name} error {process.ExitCode}).");
    }
}
