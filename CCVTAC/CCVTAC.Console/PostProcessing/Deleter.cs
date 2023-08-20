using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Deleter
{
    public static void Run(string workingDirectory, Printer printer)
    {
        var dir = new DirectoryInfo(workingDirectory);
        List<string> deletableExtensions = new() { ".json", ".jpg" };

        printer.Print($"Deleting unneeded {string.Join(" and ", deletableExtensions)} files...");

        foreach (var file in dir.EnumerateFiles("*")
                                .Where(f => deletableExtensions.Contains(f.Extension)))
        {
            try
            {
                file.Delete();
                printer.PrintLine($"Deleted \"{file.Name}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"Could not delete \"{file.Name}\": {ex.Message}");
            }
        }

        printer.Print("Deletion done.");
    }
}
