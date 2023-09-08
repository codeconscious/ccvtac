using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Mover
{
    internal static void Run(
        string workingDirectory,
        string moveToDirectory,
        YouTubePlaylistJson.Root? playlistJson,
        Printer printer,
        bool shouldOverwrite)
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        uint successCount = 0;
        uint failureCount = 0;
        var workingDirInfo = new DirectoryInfo(workingDirectory);

        var verifiedMoveToDir = playlistJson is null
            ? moveToDirectory
            : Path.Combine(moveToDirectory, playlistJson.Title); // TODO: Handle invalid chars!

        try
        {
            if (!Path.Exists(verifiedMoveToDir))
            {
                printer.Print($"Creating custom move-to directory \"{verifiedMoveToDir}\" (based on playlist name)... ", appendLineBreak: false);
                Directory.CreateDirectory(verifiedMoveToDir);
                printer.Print("OK!");
            }
        }
        catch (Exception ex)
        {
            printer.Error($"Error creating directory \"{verifiedMoveToDir}\": {ex.Message}");
            printer.Warning($"Using default move-to directory \"{moveToDirectory}\" instead");
            verifiedMoveToDir = moveToDirectory;
        }

        printer.Print($"Moving audio file(s) to \"{verifiedMoveToDir}\"...");
        foreach (var file in workingDirInfo.EnumerateFiles("*.m4a"))
        {
            try
            {
                File.Move(
                    file.FullName,
                    $"{Path.Combine(verifiedMoveToDir, file.Name)}",
                    shouldOverwrite);
                successCount++;
                printer.Print($"• Moved \"{file.Name}\"");
            }
            catch (Exception ex)
            {
                failureCount++;
                printer.Error($"• Error moving file \"{file.Name}\": {ex.Message}");
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
