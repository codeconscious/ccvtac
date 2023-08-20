using System.Diagnostics;
using CCVTAC.Console.DownloadEntities;

namespace CCVTAC.Console;
public class ExternalTools
{
    public static Result<int> Downloader(string args, IDownloadEntity downloadData, string saveDirectory, Printer printer)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        printer.PrintLine("Starting download...");
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

        using var process = Process.Start(processInfo);
        if (process is null)
        {
            return Result.Fail($"Could not start process {processFileName} -- is it installed?");
        }
        process.WaitForExit();
        printer.PrintLine($"Done downloading in {stopwatch.ElapsedMilliseconds:#,##0}ms");
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

        printer.PrintLine("Auto-trimming images...");
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

        using var process = Process.Start(processInfo);
        if (process is null)
        {
            return Result.Fail($"Could not start process {processFileName} -- is it installed?");
        }
        process.WaitForExit();
        printer.PrintLine($"Done auto-trimming in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return Result.Ok(process.ExitCode);
    }

    public static Result<int> AudioNormalization(string workingDirectory, Printer printer)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // TODO: This way won't work for M4A files. Find another method.
        const string processFileName = "find";
        const string args = """. -name "*.m4a" -exec mp3gain -r -k -p -s i {} \;""";

        printer.PrintLine($"Running command: {processFileName}");
        var processInfo = new ProcessStartInfo()
        {
            FileName = processFileName,
            Arguments = $"{args}",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using var process = Process.Start(processInfo);
        if (process is null)
        {
            return Result.Fail($"Could not start process {processFileName} -- is it installed?");
        }
        process.WaitForExit();
        printer.PrintLine($"Done in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return Result.Ok(process.ExitCode);
    }
}
