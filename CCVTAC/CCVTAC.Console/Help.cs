namespace CCVTAC.Console;

public static class Help
{
    internal static void Print(Printer printer)
    {
        printer.Print("CodeConscious Video-to-Audio Converter (CCVTAC)");
        printer.Print("• Easily convert YouTube videos to local M4A audio files with ID3v2 tags!");
        printer.Print("• Supports video, playlist, and channel URLs");
        printer.Print("• Video metadata (uploader name and URL, source URL, etc.) saved to Comment tags");
        printer.Print("• Auto-renames files via specific regex patterns (to remove media IDs, etc.)");
        printer.Print("• Video thumbnails are auto-trimmed and written to files as album art (Optional)");
        printer.Print("• Post-processed files are automatically moved to a specified directory");
        printer.Print("• All URLs entered are saved locally to a file specified in the settings",
                      appendLines: 1);

        printer.Print("Prerequisites:");
        printer.Print("• [Required] yt-dlp (https://github.com/yt-dlp/yt-dlp/) for downloading audio");
        printer.Print("• [Optional] mogrify (https://imagemagick.org/script/mogrify.php) for auto-cropping album art",
                      appendLines: 1);

        printer.Print("Instructions:");
        printer.Print("• Run the program once to generate a blank settings.json file, then populate it with directory paths.");
        printer.Print("• After the application starts, enter single video or playlist URLs to start the download process.");
        printer.Print("• Enter \"q\" or \"quit\" to quit.");
        printer.Print("• Pass \"--history\" to display recent URL history.", appendLines: 1);

        printer.Print("Please help improve this tool by reporting errors (with any entered URLs) at https://github.com/codeconscious/ccvtac/issues.");
    }
}
