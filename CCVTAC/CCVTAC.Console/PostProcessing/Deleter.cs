using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Deleter
{
    public static void Run(string workingDirectory, Printer printer)
    {
        var dir = new DirectoryInfo(workingDirectory);
        var deletableExtensions = new List<string>() { ".json", ".jpg" };

        printer.Print($"Deleting now-unnecessary {string.Join(" and ", deletableExtensions)} files...");

        foreach (var file in dir.EnumerateFiles("*")
                                .Where(f => deletableExtensions.Contains(f.Extension)))
        {
            try
            {
                file.Delete();
                printer.Print($"• Deleted \"{file.Name}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"• Could not delete \"{file.Name}\": {ex.Message}");
            }
        }

        printer.Print("Deletion completed OK.");
    }
}
