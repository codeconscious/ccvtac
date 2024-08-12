namespace CCVTAC.Console;

internal static class Commands
{
    internal static readonly char Prefix = '\\';

    internal static string[] QuitOptions { get; } =
        [MakeCommand("quit"), MakeCommand("q"), MakeCommand("exit")];

    internal static string SummaryCommand { get; } = MakeCommand("commands");

    internal static string[] SettingsSummary  { get; } = [MakeCommand("settings")];

    internal static string[] History { get; } = [MakeCommand("history")];

    internal static string[] SplitChapterToggles { get; } =
        [MakeCommand("split"), MakeCommand("toggle-split")];

    internal static string[] EmbedImagesToggles { get; } =
        [MakeCommand("images"), MakeCommand("toggle-images")];

    internal static string[] QuietModeToggles { get; } =
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
