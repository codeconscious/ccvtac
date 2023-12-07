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
    public void Append(string url, DateTime entryTime, Printer printer)
    {
        try
        {
            string serializedEntryTime = JsonSerializer.Serialize(entryTime);
            File.AppendAllText(FilePath, serializedEntryTime + Separator + url + Environment.NewLine);
            printer.Print($"Added \"{url}\" to the history log.");
        }
        catch (Exception ex)
        {
            printer.Error($"Could not append URL(s) to history log: " + ex.Message);
        }
    }
}
