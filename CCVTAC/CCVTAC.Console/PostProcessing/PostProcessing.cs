using System.IO;

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
        Printer.Print("Starting post-processing...");

        // TODO: Create an interface and iterate through them, calling `Run()`?
        ImageProcessor.Run(WorkingDirectory, Printer);
        Tagger.Run(WorkingDirectory, Printer);
        // AudioNormalizer.Run(WorkingDirectory, Printer); // TODO: `mp3gain`は無理なので、別のnormalize方法を要検討。
        Renamer.Run(WorkingDirectory, Printer);
        Deleter.Run(WorkingDirectory, Printer);
        Mover.Run(WorkingDirectory, MoveToDirectory, Printer);

        IReadOnlyList<string> ignoreFiles = new List<string>() { ".DS_Store" };
        if (Directory.GetFiles(WorkingDirectory, "*")
                     .Where(dirFile => !ignoreFiles.Any(ignoreFile => dirFile.EndsWith(ignoreFile)))
                     .Any())
        {
            Printer.Warning("Some files unexpectedly remain in the working folder. Please check it.");
        }

        Printer.Print("Post-processing done!");
    }
}
