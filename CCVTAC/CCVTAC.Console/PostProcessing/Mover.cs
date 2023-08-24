using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Mover
{
    internal static void Run(string workingDirectory, string moveToDirectory, Printer printer, bool shouldOverwrite)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        uint successCount = 0;
        uint failureCount = 0;
        var workingDirInfo = new DirectoryInfo(workingDirectory);
        printer.Print($"Moving audio files to \"{moveToDirectory}\"...");
        foreach (var file in workingDirInfo.EnumerateFiles("*.m4a"))
        {
            try
            {
                File.Move(
                    file.FullName,
                    $"{Path.Combine(moveToDirectory, file.Name)}",
                    shouldOverwrite);
                successCount++;
                printer.Print($"- Moved \"{file.Name}\"");
            }
            catch (Exception ex)
            {
                failureCount++;
                printer.Error($"- Error moving file \"{file.Name}\": {ex.Message}");
            }
        }

        printer.Print($"{successCount} file(s) moved in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
        if (failureCount > 0)
            printer.Warning($"However, {failureCount} file(s) could not be moved.");

        WarnAboutOrphanedWorkingFiles(workingDirectory, printer);
    }

    /// <summary>
    /// If the working directory contains files when it is expected to be empty,
    /// then shows a warning message to the user.
    /// </summary>
    /// <param name="workingDirectory"></param>
    /// <param name="printer"></param>
    private static void WarnAboutOrphanedWorkingFiles(string workingDirectory, Printer printer)
    {
        string[] ignoreFiles = new[] { ".DS_Store" }; // Ignore macOS system files

        var files = Directory.GetFiles(workingDirectory, "*")
                             .Where(dirFile => !ignoreFiles.Any(ignoreFile => dirFile.EndsWith(ignoreFile)));

        if (files.Any())
        {
            printer.Warning($"{files.Count()} file(s) unexpectedly remain in the working folder.");
        }
    }
}
