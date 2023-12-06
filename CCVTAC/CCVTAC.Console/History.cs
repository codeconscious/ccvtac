using System.IO;
using System.Text.Json;

namespace CCVTAC.Console;

/// <summary>
/// Handles storing, retrieving, and (eventually) analyzing data relating
/// to URLs that the user has entered.
/// </summary>
public class History
{
    private static readonly char Separator = ';';
    private string FilePath { get; init; }

    public History(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Add a URL and related data to the history file.
    /// </summary>
    public void Append(IList<string> urls, DateTime entryTime, Printer printer)
    {
        try
        {
            string serializedEntryTime = JsonSerializer.Serialize(entryTime);
            IEnumerable<string> lines = urls.Select(url => serializedEntryTime + Separator + url);
            File.AppendAllLines(FilePath, lines);
            printer.Print($"Added {urls.Count} URL{(urls.Count == 1 ? "" : "s")} to the history log.");
        }
        catch (Exception ex)
        {
            printer.Error($"Could not append URL(s) to history log: " + ex.Message);
        }
    }
}
