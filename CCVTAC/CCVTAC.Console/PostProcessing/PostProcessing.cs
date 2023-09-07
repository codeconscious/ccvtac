using CCVTAC.Console.Settings;

namespace CCVTAC.Console.PostProcessing;

public sealed class Setup
{
    public UserSettings UserSettings { get; }
    public Printer Printer { get; }

    public Setup(UserSettings userSettings, Printer printer)
    {
        UserSettings = userSettings;
        Printer = printer;
    }

    internal void Run()
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        Printer.Print("Starting post-processing...");

        // TODO: Create an interface and iterate through them, calling `Run()`?
        ImageProcessor.Run(UserSettings.WorkingDirectory, Printer);
        var tagResult = Tagger.Run(UserSettings, Printer);
        if (tagResult.IsSuccess)
        {
            Printer.Print(tagResult.Value);

            // AudioNormalizer.Run(UserSettings.WorkingDirectory, Printer); // TODO: `mp3gain`は無理なので、別のnormalize方法を要検討。
            Renamer.Run(UserSettings.WorkingDirectory, Printer);
            Deleter.Run(UserSettings.WorkingDirectory, Printer);
            Mover.Run(UserSettings.WorkingDirectory, UserSettings.MoveToDirectory, Printer, true);
        }
        else
        {
            Printer.Errors("Tagging error(s) preventing further post-processing: ", tagResult);
        }

        Printer.Print($"Post-processing done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }
}
