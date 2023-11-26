using System.Diagnostics;
using System.IO;

namespace CCVTAC.Console.ExternalUtilities;

internal static class Runner
{
    internal static Result Run(UtilitySettings settings,
                               Printer printer,
                               IDictionary<int, string>? knownExitCodes = null)
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
            RedirectStandardOutput = settings.RedirectStandardOutput,
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

        if (settings.RedirectStandardOutput)
        {
            try
            {
                string path = Path.Combine( settings.WorkingDirectory, "supplementaryOutput.txt");
                string output = process.StandardOutput.ReadToEnd();
                File.WriteAllText(path, output);
            }
            catch (Exception ex)
            {
                printer.Error("Error saving the supplementary output: " + ex.Message);
                throw;
            }
        }

        process.WaitForExit();

        printer.Print($"Done with {settings.Program.Purpose} in {stopwatch.ElapsedMilliseconds:#,##0}ms");

        int exitCode = process.ExitCode;
        if (exitCode == 0)
        {
            return Result.Ok();
        }

        if (knownExitCodes is not null &&
            knownExitCodes.ContainsKey(exitCode))
        {
            return Result.Fail($"External program \"{settings.Program.Name}\" reported error #{exitCode}: {knownExitCodes[exitCode]}.");
        }

        return Result.Fail($"External program \"{settings.Program.Name}\" reported an error (#{exitCode}).");
    }
}
