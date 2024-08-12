namespace CCVTAC.Console;

internal static class Commands
{
    internal static readonly char Prefix = '\\';

    internal static readonly string[] _quit =
        [MakeCommand("quit"), MakeCommand("q"), MakeCommand("exit")];

    internal static readonly string[] _history =
        [MakeCommand("history")];

    internal static readonly string[] _showSettings =
        [MakeCommand("settings")];

    internal static readonly string[] _toggleSplitChapter =
        [MakeCommand("split"), MakeCommand("toggle-split")];

    internal static readonly string[] _toggleEmbedImages =
        [MakeCommand("images"), MakeCommand("toggle-images")];

    internal static readonly string[] _toggleQuietMode =
        [MakeCommand("quiet"), MakeCommand("toggle-quiet")];

    internal static readonly string _updateAudioFormatPrefix = MakeCommand("format-");

    // internal static readonly string[] _deleteTempFiles =
    //     MakeCommand("delete-temp")];

    internal static string MakeCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("The text cannot be null or white space.", nameof(text));

        if (text.Contains(' '))
            throw new ArgumentException("The text should not contain any white space.", nameof(text));

        return $"{Prefix}{text}";
    }
}
