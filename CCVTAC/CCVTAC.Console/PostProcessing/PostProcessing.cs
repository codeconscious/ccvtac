namespace CCVTAC.Console.PostProcessing;

public sealed class Setup
{
    public string WorkingDirectory { get; }
    public string MoveToDirectory { get; }
    public Printer Printer { get; }

    public Setup(Settings.UserSettings settings, Printer printer)
    {
        WorkingDirectory = settings.WorkingDirectory!;
        MoveToDirectory = settings.MoveToDirectory!;
        Printer = printer;
    }

    internal void Run()
    {
        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        Printer.Print("Starting post-processing...");

        // TODO: Create an interface and iterate through them, calling `Run()`?
        ImageProcessor.Run(WorkingDirectory, Printer);
        var tagResult = Tagger.Run(WorkingDirectory, Printer);
        if (tagResult.IsSuccess)
        {
            Printer.Print(tagResult.Value);

            // AudioNormalizer.Run(WorkingDirectory, Printer); // TODO: `mp3gain`は無理なので、別のnormalize方法を要検討。
            Renamer.Run(WorkingDirectory, Printer);
            Deleter.Run(WorkingDirectory, Printer);
            Mover.Run(WorkingDirectory, MoveToDirectory, Printer, true);
        }
        else
        {
            Printer.Errors("Tagging error(s) preventing further post-processing: ", tagResult);
        }

        Printer.Print($"Post-processing done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }
}
