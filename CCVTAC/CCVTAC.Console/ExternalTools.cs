using System.Diagnostics;
using System.IO;
using CCVTAC.Console.DownloadEntities;

namespace CCVTAC.Console;
public class ExternalTools
{
    public static void Downloader()
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?view=net-7.0&redirectedfrom=MSDN#System_Diagnostics_Process_StandardOutput
        using (Process process = new())
        {
            process.StartInfo.FileName = "ls";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WorkingDirectory = "./temp";
            process.Start();

            // Synchronously read the standard output of the spawned process.
            StreamReader reader = process.StandardOutput;
            string output = reader.ReadToEnd();

            // Write the redirected output to this application's window.
            System.Console.WriteLine(output);

            process.WaitForExit();
        }
    }

    public static Result<int> Downloader(string args, IDownloadEntity downloadData, string saveDirectory, Printer printer)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        const string processFileName = "yt-dlp";
        printer.PrintLine($"Running command: {processFileName} {args} {downloadData.FullResourceId}");
        var processInfo = new ProcessStartInfo()
        {
            FileName = processFileName,
            Arguments = $"{args} {downloadData.FullResourceId}",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            CreateNoWindow = true,
            WorkingDirectory = saveDirectory
        };

        var process = Process.Start(processInfo);
        if (process is null)
        {
            return Result.Fail($"Could not start process {processFileName} -- is it installed?");
        }
        process.WaitForExit();
        printer.PrintLine($"Done in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return process.ExitCode == 0
            ? Result.Ok(process.ExitCode)
            : Result.Fail($"Error downloading the resource (error code {process.ExitCode}).");
    }

    public static Result<int> ImageProcessor(string workingDirectory, Printer printer)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        const string processFileName = "mogrify";
        const string args = "-trim -fuzz 10% *.jpg";

        printer.PrintLine($"Running command: {processFileName} {args}");
        var processInfo = new ProcessStartInfo()
        {
            FileName = processFileName,
            Arguments = $"{args}",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        var process = Process.Start(processInfo);
        if (process is null)
        {
            return Result.Fail($"Could not start process {processFileName} -- is it installed?");
        }
        process.WaitForExit();
        printer.PrintLine($"Done in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return Result.Ok(process.ExitCode);
    }

    public static Result<int> AudioNormalization(string workingDirectory, Printer printer)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        const string processFileName = "find";
        const string args = """. -name "*.m4a" -exec mp3gain -r -k -p -s i {} \;""";

        printer.PrintLine($"Running command: {processFileName}");
        var processInfo = new ProcessStartInfo()
        {
            FileName = processFileName,
            Arguments = $"{args}",
            // Arguments = string.Empty,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        var process = Process.Start(processInfo);
        if (process is null)
        {
            return Result.Fail($"Could not start process {processFileName} -- is it installed?");
        }
        process.WaitForExit();
        printer.PrintLine($"Done in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return Result.Ok(process.ExitCode);
    }
}
