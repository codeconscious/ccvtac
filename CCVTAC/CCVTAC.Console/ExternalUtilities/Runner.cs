using System.Diagnostics;
using System.IO;

namespace CCVTAC.Console.ExternalUtilities;

internal static class Runner
{
    internal static Result<int> Run(UtilitySettings settings, Printer printer)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        printer.Print($"Starting {settings.Program.Name} for {settings.Program.Purpose}...");
        printer.Print($"Running command: {settings.Program.Name} {settings.Args}");

        ProcessStartInfo processStartInfo = new()
        {
            FileName = settings.Program.Name,
            Arguments = settings.Args,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true,
            WorkingDirectory = settings.WorkingDirectory
        };

        using Process? process = Process.Start(processStartInfo);

        if (process is null)
        {
            return Result.Fail($"Could not start {settings.Program.Name}. " +
                               $"Please install it from {settings.Program.Url}.");
        }

        process.WaitForExit();

        printer.Print($"Done with {settings.Program.Purpose} in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return process.ExitCode == 0
            ? Result.Ok(process.ExitCode)
            : Result.Fail($"Full or partial download error ({settings.Program.Name} error {process.ExitCode}).");
    }
}
