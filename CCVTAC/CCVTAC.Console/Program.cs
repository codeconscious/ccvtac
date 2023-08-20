using System;
using System.IO;
using CCVTAC.Console.DownloadEntities;
using CCVTAC.Console.Settings;

namespace CCVTAC.Console;

class Program
{
    private static readonly string[] QuitCommands = new[] { "q", "quit", "exit" };

    static void Main(string[] args)
    {
        var printer = new Printer();

        if (args.Length > 0 && args[0] == "-h")
        {
            PrintHelp(printer);
            return;
        }

        var readSettingsResult = SettingsService.Read(printer, createFileIfMissing: true);
        if (readSettingsResult.IsFailed)
        {
            printer.Error(readSettingsResult.Errors[0].Message);
            return;
        }
        Settings.Settings settings = readSettingsResult.Value;

        var ensureValidSettingsResult = SettingsService.EnsureValidSettings(settings);
        if (ensureValidSettingsResult.IsFailed)
        {
            printer.Error("Error(s) found in settings file:");
            ensureValidSettingsResult.Errors.ForEach(e => printer.Error($"- {e.Message}"));
            return;
        }
        printer.PrintLine("Settings file loaded OK.");

        SettingsService.SetId3v2Version(
            version: SettingsService.Id3v2Version.TwoPoint3,
            forceAsDefault: true);

        while (true)
        {
            string input = printer.GetInput("Enter a YouTube URL (or 'q' to quit): ");

            if (QuitCommands.Contains(input)) // TODO: Make case-insensitive.
            {
                printer.PrintLine("Quitting...");
                return;
            }

            Run(input, settings, printer);
        }
    }

    static void Run(string url, Settings.Settings settings, Printer printer)
    {
        var downloadEntityResult = DownloadEntityFactory.Create(url);
        if (downloadEntityResult.IsFailed)
        {
            printer.Error(downloadEntityResult.Errors?.First().Message
                          ?? "An unknown error occurred parsing the resource type.");
            return;
        }
        var downloadEntity = downloadEntityResult.Value;
        printer.PrintLine($"Processing {downloadEntity.GetType()} URL...");

        List<string> args = new() {
            "--extract-audio -f m4a",
            "--write-thumbnail",
            "--convert-thumbnails jpg",
            "--write-info-json",
            "--split-chapters"
        };

        var downloadResult = ExternalTools.Downloader(
            string.Join(" ", args),
            downloadEntity,
            settings.WorkingDirectory!,
            printer);
        if (downloadResult.IsFailed)
        {
            printer.Error(downloadResult.Errors.First().Message);
            return;
        }

        printer.Print("Adding URL to the history log... ");
        try
        {
            File.AppendAllText("history.log", url + Environment.NewLine);
        }
        catch (Exception ex)
        {
            printer.Error("Error appending to history log: " + ex.Message);
        }
        printer.PrintLine("OK.");


        var pp = new PostProcessing.PostProcessing(settings, printer);
        pp.Run();
    }

    static void PrintHelp(Printer printer)
    {
        printer.PrintLine("CodeConscious Video-to-Audio Converter (ccvtac)");
        printer.PrintLine("• Converts YouTube videos to MP3s saved locally on your device");
        printer.PrintLine("• Is a wrapper around yt-dlp (https://github.com/yt-dlp/yt-dlp/), which must already be installed");
        printer.PrintLine("• Adds ID3v2 tags including video thumbnails as album art and a comment summarizing video metadata");
        printer.PrintLine("Instructions:");
        printer.PrintLine("• When starting the program, optionally supply one or more of the following argument(s):");
        printer.PrintLine("    -s   Splits video chapters into separate files (Continues until you quit)");
        //                     -D   Debug mode, in which no downloads occur
        printer.PrintLine("• After the application start, enter single URLs to start the download and conversion process.");
        printer.PrintLine("  URLs may be for single videos, playlists, or channels.");
        printer.PrintLine("• Enter `q` or `quit` to exit.");
    }
}
