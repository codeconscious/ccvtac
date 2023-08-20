namespace CCVTAC.Console.PostProcessing;

internal static class ImageProcessor
{
    internal static void Run(string workingDirectory, Printer printer)
    {
        ExternalTools.ImageProcessor(workingDirectory, printer);
    }
}

