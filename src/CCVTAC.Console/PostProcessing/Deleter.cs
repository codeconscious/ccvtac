using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Deleter
{
    public static void Run(ICollection<string> filesToDelete, bool verbose, Printer printer)
    {
        List<string> deletableExtensions = [".json", ".jpg"];

        if (verbose)
            printer.Print($"Deleting temporary {string.Join(" and ", deletableExtensions)} files...");

        foreach (var fileName in filesToDelete)
        {
            try
            {
                File.Delete(fileName);

                if (verbose)
                    printer.Print($"• Deleted \"{fileName}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"• Deletion error: {ex.Message}");
            }
        }

        printer.Print("Deleted temporary files.");
    }

    public static void CheckRemaining(string workingDirectory, Printer printer)
    {
        var tempFiles = IoUtilties.Directories.GetDirectoryFiles(workingDirectory);
        if (tempFiles.Any())
        {
            printer.Warning($"{tempFiles.Count} file(s) unexpectedly remain in the working folder:");
            tempFiles.ForEach(file => printer.Warning($"• {file}"));
        }
    }
}
