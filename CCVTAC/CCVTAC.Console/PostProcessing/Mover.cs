using System.Diagnostics;
using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Mover
{
    internal static void Run(string workingDirectory,
                             string moveToDirectory,
                             CollectionMetadata? maybeCollectionData,
                             bool shouldOverwrite,
                             Printer printer)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        uint successCount = 0;
        uint failureCount = 0;
        DirectoryInfo workingDirInfo = new(workingDirectory);

        // Create a subdirectory if this is a collection (playlist or channel) download.
        string verifiedMoveToDir = maybeCollectionData is CollectionMetadata collectionData
            ? Path.Combine(
                moveToDirectory,
                $"{collectionData.Uploader} - {collectionData.Title}".ReplaceInvalidPathChars())
            : moveToDirectory;

        try
        {
            if (!Path.Exists(verifiedMoveToDir))
            {
                printer.Print($"Creating move-to directory \"{verifiedMoveToDir}\" (based on playlist metadata)... ",
                              appendLineBreak: false);
                Directory.CreateDirectory(verifiedMoveToDir);
                printer.Print("OK!");
            }
        }
        catch (Exception ex)
        {
            printer.Error($"Error creating move-to directory \"{verifiedMoveToDir}\": {ex.Message}");
            throw;
        }

        printer.Print($"Moving audio file(s) to \"{verifiedMoveToDir}\"...");
        foreach (FileInfo file in workingDirInfo.EnumerateFiles("*.m4a"))
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
        {
            printer.Warning($"However, {failureCount} file(s) could not be moved.");
        }

        var tempFiles = IoUtilties.Directories.GetDirectoryFiles(workingDirectory);
        if (tempFiles.Any())
        {
            printer.Warning($"{tempFiles.Count} file(s) unexpectedly remain in the working folder:");
            tempFiles.ForEach(file => printer.Print($"• {file}"));
        }
    }
}
