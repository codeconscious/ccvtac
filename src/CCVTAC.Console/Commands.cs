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

    internal static readonly string _updateAudioQualityPrefix = MakeCommand("quality-");

    internal static readonly string _showSummary = MakeCommand("commands");

    internal static readonly Dictionary<string, string> Summary = new()
    {
        { _history[0], "See the most recently entered URLs" },
        { _toggleSplitChapter[0], "Toggles chapter splitting for the current session only" },
        { _toggleEmbedImages[0], "Toggles image embedding for the current session only" },
        { _toggleQuietMode[0], "Toggles quiet mode for the current session only" },
        { _updateAudioFormatPrefix, $"Followed by a supported audio format (e.g., {_updateAudioFormatPrefix}m4a), changes the audio format for the current session only" },
        { _updateAudioQualityPrefix, $"Followed by a supported audio quality (e.g., {_updateAudioQualityPrefix}0), changes the audio quality for the current session only" },
        { _quit[0], "Quit the application" },
    };

    internal static string MakeCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("The text cannot be null or white space.", nameof(text));

        if (text.Contains(' '))
            throw new ArgumentException("The text should not contain any white space.", nameof(text));

        return $"{Prefix}{text}";
    }
}
