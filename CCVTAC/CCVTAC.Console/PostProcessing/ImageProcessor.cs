namespace CCVTAC.Console.PostProcessing;

internal static class ImageProcessor
{
    internal static void Run(string workingDirectory, Printer printer)
    {
        var imageEditToolSettings = new ExternalUtilties.ExternalToolSettings(
            "image cropping",
            "mogrify",
            "-trim -fuzz 10% *.jpg",
            workingDirectory,
            printer
        );
        ExternalUtilties.Caller.Run(imageEditToolSettings);
    }
}
