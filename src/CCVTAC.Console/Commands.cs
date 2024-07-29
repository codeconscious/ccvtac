namespace CCVTAC.Console;

internal static class Commands
{
    internal static readonly char CommandPrefix = '\\';

    internal static readonly string[] _historyCommands =
        [MakeCommand("history")];

    internal static readonly string[] _showSettingsCommands =
        [MakeCommand("settings")];

    internal static readonly string[] _toggleSplitChapterCommands =
        [MakeCommand("split"), MakeCommand("toggle-split")];

    internal static readonly string[] _toggleEmbedImagesCommands =
        [MakeCommand("images"), MakeCommand("toggle-images")];

    internal static readonly string[] _toggleVerboseOutputCommands =
        [MakeCommand("verbose"), MakeCommand("toggle-verbose")];

    internal static readonly string[] _quitCommands =
        [MakeCommand("quit"), MakeCommand("q"), MakeCommand("exit")];

    internal static string InputPrompt =
        $"Enter one or more YouTube media URLs, {_historyCommands[0]}, or {_quitCommands[0]} ({_quitCommands[1]}):\n▶︎";

    internal static string MakeCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("The text cannot be null or white space.", nameof(text));

        if (text.Contains(' '))
            throw new ArgumentException("The text should not contain any white space.", nameof(text));

        return $"{CommandPrefix}{text}";
    }
}
