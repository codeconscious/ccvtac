using Spectre.Console;

namespace CCVTAC.Console;

public class Printer
{
    public void Print(string message, bool appendLineBreak = true, byte prependLines = 0, byte appendLines = 0, bool processMarkup = false)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message), "Message cannot be empty.");

        PrintEmptyLines(prependLines);

        if (processMarkup)
            AnsiConsole.Markup(message);
        else
            AnsiConsole.Write(message);

        if (appendLineBreak) AnsiConsole.WriteLine();

        PrintEmptyLines(appendLines);
    }

    public void Error(string message, byte appendLines = 0)
    {
        AnsiConsole.MarkupLineInterpolated($"[red]{message}[/]");
        PrintEmptyLines(appendLines);
    }

    public void Errors(IEnumerable<string> errors, string? message = null, byte appendLines = 0)
    {
        if (errors?.Any() != true)
            throw new ArgumentException("No errors to print were provided.", nameof(errors));

        if (message is not null)
            Print("[red]" + message + "[/]", appendLines: appendLines, processMarkup: true);

        foreach (var error in errors)
            AnsiConsole.MarkupLineInterpolated($"[red]- {error}[/]");
    }

    public void Warning(string message, byte appendLines = 0)
    {
        Print("[yellow]" + message + "[/]", true, appendLines: appendLines, processMarkup: true);
    }

    /// <summary>
    /// Prints the requested number of blank lines.
    /// </summary>
    /// <param name="count"></param>
    private static void PrintEmptyLines(byte count)
    {
        if (count == 0)
            return;

        AnsiConsole.WriteLine(
            string.Concat(
                Enumerable.Repeat(Environment.NewLine, count - 1)));
    }

    public string GetInput(string prompt)
    {
        PrintEmptyLines(1);
        return AnsiConsole.Ask<string>($"[aqua]{prompt}[/]");
    }
}
