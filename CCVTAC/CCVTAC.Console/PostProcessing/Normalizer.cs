namespace CCVTAC.Console.PostProcessing;

internal static class AudioNormalizer
{
    internal static void Run(string workingDirectory, Printer printer)
    {
        ExternalTools.AudioNormalization(workingDirectory, printer);
    }
}

