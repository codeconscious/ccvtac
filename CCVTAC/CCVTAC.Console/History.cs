using System.IO;

namespace CCVTAC.Console;

public static class History
{
    public static void Append(string url, Printer printer)
    {
        try
        {
            File.AppendAllText("history.log", url + Environment.NewLine);
            printer.Print("Added URL to the history log.");
        }
        catch (Exception ex)
        {
            printer.Error($"Could not append URL {url} to history log: " + ex.Message);
        }
    }
}
