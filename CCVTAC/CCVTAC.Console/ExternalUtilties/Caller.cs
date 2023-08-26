using System.Diagnostics;

namespace CCVTAC.Console.ExternalUtilties;

public static class Caller
{
    public static Result<int> Run(ExternalToolSettings settings)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        settings.Printer.Print($"Starting {settings.Summary}...");
        settings.Printer.Print($"Running command: {settings.ProgramName} {settings.Args}");
        var processInfo = new ProcessStartInfo()
        {
            FileName = settings.ProgramName,
            Arguments = settings.Args,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            CreateNoWindow = true,
            WorkingDirectory = settings.WorkingDirectory
        };

        using var process = Process.Start(processInfo);
        if (process is null)
        {
            return Result.Fail($"Could not start {settings.ProgramName} -- is it installed?");
        }
        process.WaitForExit();
        settings.Printer.Print($"Done {settings.Summary} in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return process.ExitCode == 0
            ? Result.Ok(process.ExitCode)
            : Result.Fail($"Full or partial download error ({settings.ProgramName} error {process.ExitCode}).");
    }
}
