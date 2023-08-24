namespace CCVTAC.Console.PostProcessing;

public class Setup
{
    public string WorkingDirectory { get; }
    public string MoveToDirectory { get; }
    public Printer Printer { get; }

    public Setup(Settings.Settings settings, Printer printer)
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
        Tagger.Run(WorkingDirectory, Printer);
        // AudioNormalizer.Run(WorkingDirectory, Printer); // TODO: `mp3gain`は無理なので、別のnormalize方法を要検討。
        Renamer.Run(WorkingDirectory, Printer);
        Deleter.Run(WorkingDirectory, Printer);
        Mover.Run(WorkingDirectory, MoveToDirectory, Printer, true);

        Printer.Print($"Post-processing done in {stopwatch.ElapsedMilliseconds:#,##0}ms.");
    }
}
