using Spectre.Console;

namespace CCVTAC.Console;

public sealed class Printer
{
    public void Print(
        string message,
        bool appendLineBreak = true,
        byte prependLines = 0,
        byte appendLines = 0,
        bool processMarkup = false)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message), "Message cannot be empty.");

        PrintEmptyLines(prependLines);

        if (processMarkup)
        {
            AnsiConsole.Markup(message);
        }
        else
        {
            // `AnsiConsole.Write()` calls an internal function that uses format strings,
            // so we must duplicate any curly brackets to safely escape the message text.
            // See https://github.com/spectreconsole/spectre.console/issues/1495.
            var safeMessage = message.Replace("{", "{{").Replace("}", "}}");
            AnsiConsole.Write(safeMessage.EscapeMarkup());
        }

        if (appendLineBreak)
            AnsiConsole.WriteLine();

        PrintEmptyLines(appendLines);
    }

    public void Print(Table table)
    {
        AnsiConsole.Write(table);
    }

    public void Error(string message, byte appendLines = 0)
    {
        AnsiConsole.MarkupLineInterpolated($"[red]{message}[/]");
        PrintEmptyLines(appendLines);
    }

    public void Errors(IEnumerable<string> errors, byte appendLines = 0)
    {
        if (errors?.Any() != true)
            throw new ArgumentException("No errors were provided!", nameof(errors));

        errors.ToList().ForEach(e =>
            AnsiConsole.MarkupLineInterpolated($"[red]- {e}[/]"));
    }

    public void Errors(string headerMessage, IEnumerable<string> errors, byte appendLines = 0)
    {
        if (headerMessage is not null)
        {
            Print("[red]" + headerMessage.EscapeMarkup() + "[/]",
                  appendLines: appendLines,
                  processMarkup: true);
        }

        Errors(errors, appendLines);
    }

    public void Errors<T>(Result<T> failResult, byte appendLines = 0)
    {
        Errors(failResult.Errors.Select(e => e.Message), appendLines);
    }

    public void Errors<T>(string headerMessage, Result<T> failingResult, byte appendLines = 0)
    {
        Errors(headerMessage, failingResult.Errors.Select(e => e.Message), appendLines);
    }

    public void FirstError(IResultBase failResult, string? prepend = null)
    {
        string pre = prepend is null ? string.Empty : $"{prepend} ";
        string message = failResult?.Errors?.FirstOrDefault()?.Message ?? string.Empty;

        Error($"{pre}{message}");
    }

    public void Warning(string message, byte appendLines = 0)
    {
        // Print("[yellow]" + message + "[/]", true, appendLines: appendLines, processMarkup: true);
        AnsiConsole.MarkupLineInterpolated($"[yellow]{message}[/]");
        PrintEmptyLines(appendLines);
    }

    /// <summary>
    /// Prints the requested number of blank lines.
    /// </summary>
    /// <param name="count"></param>
    public void PrintEmptyLines(byte count)
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
        return AnsiConsole.Ask<string>($"[skyblue1]{prompt}[/]");
    }

    public string Ask(string title, string[] options)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(title)
                .AddChoices(options));
    }

    public bool AskToBool(string title, string trueAnswer, string falseAnswer) =>
        Ask(title, [trueAnswer, falseAnswer]) == trueAnswer;
}
