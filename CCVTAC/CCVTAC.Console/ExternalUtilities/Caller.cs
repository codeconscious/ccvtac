using System.Diagnostics;

namespace CCVTAC.Console.ExternalUtilties;

public static class Caller
{
    public static Result<int> Run(ExternalToolSettings toolSettings)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        toolSettings.Printer.Print($"Starting {toolSettings.Description}...");
        toolSettings.Printer.Print($"Running command: {toolSettings.ProgramName} {toolSettings.Args}");

        var processInfo = new ProcessStartInfo()
        {
            FileName = toolSettings.ProgramName,
            Arguments = toolSettings.Args,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            CreateNoWindow = true,
            WorkingDirectory = toolSettings.WorkingDirectory
        };

        using var process = Process.Start(processInfo);
        if (process is null)
        {
            return Result.Fail($"Could not start {toolSettings.ProgramName} -- is it installed?");
        }
        process.WaitForExit();
        toolSettings.Printer.Print($"Done with {toolSettings.Description} in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return process.ExitCode == 0
            ? Result.Ok(process.ExitCode)
            : Result.Fail($"Full or partial download error ({toolSettings.ProgramName} error {process.ExitCode}).");
    }
}
