using System.Diagnostics;

namespace CCVTAC.Console.ExternalTools;

internal static class Runner
{
    internal static Result Run(ToolSettings settings, Printer printer)
    {
        Watch watch = new();

        printer.Info($"Starting {settings.Program.Name} for {settings.Program.Purpose}...");
        printer.Debug($"Running command: {settings.Program.Name} {settings.Args}");

        ProcessStartInfo processStartInfo = new()
        {
            FileName = settings.Program.Name,
            Arguments = settings.Args,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = settings.WorkingDirectory
        };

        using Process? process = Process.Start(processStartInfo);

        if (process is null)
        {
            return Result.Fail($"Could not locate {settings.Program.Name}. If it's not installed, please install from {settings.Program.Url}.");
        }


        string error = process.StandardError.ReadToEnd(); // Must precede `WaitForExit()`.
        process.WaitForExit();

        printer.Info($"Completed {settings.Program.Purpose} in {watch.ElapsedFriendly}.");

        int exitCode = process.ExitCode;
        if (exitCode == 0)
        {
            return Result.Ok();
        }

        return Result.Fail($"[{settings.Program.Name}] {error.Replace(Environment.NewLine, string.Empty)}.");
    }
}
