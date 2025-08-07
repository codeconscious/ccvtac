namespace CCVTAC.Console;

internal static class Commands
{
    internal const char Prefix = '\\';

    internal static string[] QuitCommands { get; } =
        [MakeCommand("quit"), MakeCommand("q"), MakeCommand("exit")];

    internal static string HelpCommand { get; } = MakeCommand("help");

    internal static string[] SettingsSummary { get; } = [MakeCommand("settings")];

    internal static string[] History { get; } = [MakeCommand("history")];
    internal static string[] UpdateDownloader { get; } = [MakeCommand("update-downloader"), MakeCommand("update-dl")];

    internal static string[] SplitChapterToggles { get; } =
        [MakeCommand("split"), MakeCommand("toggle-split")];

    internal static string[] EmbedImagesToggles { get; } =
        [MakeCommand("images"), MakeCommand("toggle-images")];

    internal static string[] QuietModeToggles { get; } =
        [MakeCommand("quiet"), MakeCommand("toggle-quiet")];

    internal static string UpdateAudioFormatPrefix { get; } = MakeCommand("format-");

    internal static string UpdateAudioQualityPrefix { get; } = MakeCommand("quality-");

    internal static Dictionary<string, string> Summary { get; } =
        new()
        {
            { string.Join(" or ", History), "See the most recently entered URLs" },
            { string.Join(" or ", SplitChapterToggles), "Toggles chapter splitting for the current session only" },
            { string.Join(" or ", EmbedImagesToggles), "Toggles image embedding for the current session only" },
            { string.Join(" or ", QuietModeToggles), "Toggles quiet mode for the current session only" },
            { string.Join(" or ", UpdateDownloader), "Updates the downloader using the command specified in the settings" },
            {
                UpdateAudioFormatPrefix,
                $"Followed by a supported audio format (e.g., {UpdateAudioFormatPrefix}m4a), changes the audio format for the current session only"
            },
            {
                UpdateAudioQualityPrefix,
                $"Followed by a supported audio quality (e.g., {UpdateAudioQualityPrefix}0), changes the audio quality for the current session only"
            },
            { string.Join(" or ", QuitCommands), "Quit the application" },
            { string.Join(" or ", HelpCommand), "See this help message" },
        };

    private static string MakeCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("The text cannot be null or white space.", nameof(text));

        if (text.Contains(' '))
            throw new ArgumentException(
                "The text should not contain any white space.",
                nameof(text)
            );

        return $"{Prefix}{text}";
    }
}
