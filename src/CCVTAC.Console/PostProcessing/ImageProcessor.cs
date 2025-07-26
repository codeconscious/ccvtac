using CCVTAC.Console.ExternalTools;

namespace CCVTAC.Console.PostProcessing;

internal static class ImageProcessor
{
    internal static readonly string ProgramName = "mogrify";

    internal static void Run(string workingDirectory, Printer printer)
    {
        ToolSettings imageEditToolSettings = new($"{ProgramName} -trim -fuzz 10% *.jpg", workingDirectory);

        Runner.Run(imageEditToolSettings, [], printer);
    }
}
