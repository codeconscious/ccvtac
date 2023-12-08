using System.IO;
using System.Text.Json;
using Spectre.Console;

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
            string serializedEntryTime = JsonSerializer.Serialize(entryTime).Replace("\"", "");
            File.AppendAllText(FilePath, serializedEntryTime + Separator + url + Environment.NewLine);
            printer.Print($"Added \"{url}\" to the history log.");
        }
        catch (Exception ex)
        {
            printer.Error($"Could not append URL {url} to history log: " + ex.Message);
        }
    }

    public void PrintRecent(Printer printer)
    {
        try
        {
            IEnumerable<IGrouping<DateTime, string>> historyData =
                File.ReadAllLines(FilePath)
                    .TakeLast(25)
                    .Select(line => line.Split(Separator))
                    .Where(lineItems => lineItems.Length == 2)
                    .GroupBy(line => DateTime.Parse(line[0]), line => line[1]);

            Table table = new();
            table.AddColumns("Time", "URL");

            string formattedTime, urls;
            foreach (var thisDate in historyData)
            {
                formattedTime = $"{thisDate.Key:dd MMMM yyyy HH:mm:ss}";
                urls = string.Join(Environment.NewLine, thisDate);
                table.AddRow(formattedTime, urls);
            }

            printer.Print(table);
        }
        catch (Exception ex)
        {
            printer.Error($"Could not print recent history: {ex.Message}");
        }
    }
}
