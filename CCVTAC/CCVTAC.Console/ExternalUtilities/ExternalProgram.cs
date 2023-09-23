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
        var processInfo = new ProcessStartInfo()
        {
            FileName = this.Name,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            CreateNoWindow = true,
        };

        try
        {
            using var process = Process.Start(processInfo);
            return Result.Ok();
        }
        catch (Exception)
        {
            return Result.Fail($"The program \"{Name}\" is not installed.");
        }
    }
}
