namespace CCVTAC.Console.PostProcessing;

internal static class ImageProcessor
{
    internal static void Run(string workingDirectory, Printer printer)
    {
        var imageEditToolSettings = new ExternalTools.ExternalToolSettings(
            "image cropping",
            "mogrify",
            "-trim -fuzz 10% *.jpg",
            workingDirectory,
            printer
        );
        ExternalTools.Run(imageEditToolSettings);
    }
}
