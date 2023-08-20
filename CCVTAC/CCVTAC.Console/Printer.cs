using Spectre.Console;

namespace CCVTAC.Console;

public class Printer
{
    public void Print(string message)
    {
        AnsiConsole.Write(message);
    }

    public void PrintLine(string message)
    {
        AnsiConsole.WriteLine(message);
    }

    public void Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]{message}[/]");
    }

    public string GetInput(string prompt)
    {
        return AnsiConsole.Ask<string>(prompt);
    }
}
