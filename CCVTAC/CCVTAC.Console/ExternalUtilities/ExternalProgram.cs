using System.Diagnostics;

namespace CCVTAC.Console.ExternalUtilities;

internal record ExternalProgram
{
    internal string Name { get; init; }
    internal string Url { get; }

    internal ExternalProgram(string name, string url)
    {
        Name = name;
        Url = url;
    }

    internal Result ProgramExists()
    {
        var processStartInfo = new ProcessStartInfo()
        {
            FileName = this.Name,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        try
        {
            using var process = Process.Start(processStartInfo);
            if (process is null)
            {
                return Result.Fail($"The program \"{Name}\" was not found. (The process was null.)");
            }
            process.WaitForExit();
            return Result.Ok();
        }
        catch (Exception)
        {
            return Result.Fail($"The program \"{Name}\" was not found.");
        }
    }
}
