using System.Diagnostics;
using CCVTAC.Console.DownloadEntities;

namespace CCVTAC.Console;
public class ExternalTools
{
    public static Result<int> Downloader(
        string args,
        IDownloadEntity downloadData,
        string saveDirectory,
        Printer printer)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        printer.Print("Starting download...");
        const string processFileName = "yt-dlp";
        printer.Print($"Running command: {processFileName} {args} {downloadData.FullResourceId}");
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
            return Result.Fail($"Could not start {processFileName} -- is it installed?");
        }
        process.WaitForExit();
        printer.Print($"Done downloading in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return process.ExitCode == 0
            ? Result.Ok(process.ExitCode)
            : Result.Fail($"Full or partial download error (yt-dlp error {process.ExitCode}).");
    }

    public static Result<int> ImageProcessor(string workingDirectory, Printer printer)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        const string processFileName = "mogrify";
        const string args = "-trim -fuzz 10% *.jpg";

        printer.Print("Auto-trimming images...");
        printer.Print($"Running command: {processFileName} {args}");
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
        printer.Print($"Done auto-trimming in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return Result.Ok(process.ExitCode);
    }

    public static Result<int> AudioNormalization(string workingDirectory, Printer printer)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // TODO: This way won't work for M4A files. Find another method.
        const string processFileName = "find";
        const string args = """. -name "*.m4a" -exec mp3gain -r -k -p -s i {} \;""";

        printer.Print($"Running command: {processFileName}");
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
        printer.Print($"Done in {stopwatch.ElapsedMilliseconds:#,##0}ms");
        return Result.Ok(process.ExitCode);
    }
}
