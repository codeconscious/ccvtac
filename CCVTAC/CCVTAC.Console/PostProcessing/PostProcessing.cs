namespace CCVTAC.Console.PostProcessing;

public class Setup
{
    public string WorkingDirectory { get; }
    public string MoveToDirectory { get; }
    public Printer Printer { get; }

    public Setup(Settings.Settings setting, Printer printer)
    {
        WorkingDirectory = setting.WorkingDirectory!;
        MoveToDirectory = setting.MoveToDirectory!;
        Printer = printer;
    }

    internal void Run()
    {
        Printer.PrintLine("Starting post-processing...");

        // TODO: Create an interface and iterate through them, calling `Run()`?
        ImageProcessor.Run(WorkingDirectory, Printer);
        Tagger.Run(WorkingDirectory, Printer);
        // AudioNormalizer.Run(WorkingDirectory, Printer); // TODO: `mp3gain`は無理なので、別のnormalize方法を要検討。
        Deleter.Run(WorkingDirectory, Printer);
        Mover.Run(WorkingDirectory, MoveToDirectory, Printer);

        Printer.PrintLine("Post-processing done!");
    }
}
