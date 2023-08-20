using System;
using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Deleter
{
    public static void Run(string workingDirectory, Printer printer)
    {
        try
        {
            var dir = new DirectoryInfo(workingDirectory);
            List<string> deletableExtensions = new() { ".json", ".jpg" };

            foreach (var file in dir.EnumerateFiles("*")
                                    .Where(f => deletableExtensions.Contains(f.Extension)))
            {
                file.Delete();
                printer.PrintLine($"Deleted file \"{file.Name}\"");
            }
        }
        catch (Exception ex)
        {
            printer.Error($"Error deleting file: {ex.Message}");
        }
    }
}

