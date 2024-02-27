using System.Diagnostics;

namespace CCVTAC.Console.ExternalTools;

internal static class Runner
{
    internal static Result Run(ToolSettings settings, Printer printer)
    {
        Watch watch = new();

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

        printer.Print($"Done with {settings.Program.Purpose} in {watch.ElapsedFriendly}.");

        int exitCode = process.ExitCode;
        if (exitCode == 0)
        {
            return Result.Ok();
        }

        if (settings.ExitCodes is not null &&
            settings.ExitCodes.ContainsKey(exitCode))
        {
            return Result.Fail($"External program \"{settings.Program.Name}\" reported error {exitCode}: {settings.ExitCodes[exitCode]}.");
        }

        return Result.Fail($"External program \"{settings.Program.Name}\" reported an error ({exitCode}).");
    }
}
