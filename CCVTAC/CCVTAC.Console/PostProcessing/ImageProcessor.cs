namespace CCVTAC.Console.PostProcessing;

internal static class ImageProcessor
{
    internal static void Run(string workingDirectory, Printer printer)
    {
        var imageEditToolSettings = new ExternalUtilties.UtilitySettings(
            "image cropping",
            "mogrify",
            "-trim -fuzz 10% *.jpg",
            workingDirectory
        );

        ExternalUtilties.Caller.Run(imageEditToolSettings, printer);
    }
}
