using System.Diagnostics;

namespace CCVTAC.Console.ExternalUtilties;

public static class Caller
{
    public static Result<int> Run(UtilitySettings toolSettings, Printer printer)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        printer.Print($"Starting {toolSettings.Description}...");
        printer.Print($"Running command: {toolSettings.ProgramName} {toolSettings.Args}");

        var processStartInfo = new ProcessStartInfo()
        {
            FileName = toolSettings.ProgramName,
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
            return Result.Fail($"Could not start {toolSettings.ProgramName} -- is it installed?");
        }
        process.WaitForExit();

        printer.Print($"Done with {toolSettings.Description} in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return process.ExitCode == 0
            ? Result.Ok(process.ExitCode)
            : Result.Fail($"Full or partial download error ({toolSettings.ProgramName} error {process.ExitCode}).");
    }
}
