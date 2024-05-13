using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Deleter
{
    public static void Run(string workingDirectory, bool verbose, Printer printer)
    {
        DirectoryInfo dir = new(workingDirectory);
        List<string> deletableExtensions = [".json", ".jpg"];

        if (verbose)
            printer.Print($"Deleting temporary {string.Join(" and ", deletableExtensions)} files...");

        foreach (FileInfo file in dir.EnumerateFiles("*")
                                     .Where(f => deletableExtensions.Contains(f.Extension)))
        {
            try
            {
                file.Delete();

                if (verbose)
                    printer.Print($"• Deleted \"{file.Name}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"• Could not delete \"{file.Name}\": {ex.Message}");
            }
        }

        printer.Print("Deleted temporary files.");

        var tempFiles = IoUtilties.Directories.GetDirectoryFiles(workingDirectory);
        if (tempFiles.Any())
        {
            printer.Warning($"{tempFiles.Count} file(s) unexpectedly remain in the working folder:");
            tempFiles.ForEach(file => printer.Print($"• {file}"));
        }
    }
}
