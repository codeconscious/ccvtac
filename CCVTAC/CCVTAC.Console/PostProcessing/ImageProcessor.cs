using CCVTAC.Console.ExternalUtilities;

namespace CCVTAC.Console.PostProcessing;

internal static class ImageProcessor
{
    internal static void Run(string workingDirectory, Printer printer)
    {
        var imageProgram = new ExternalProgram(
            "mogrify",
            "https://imagemagick.org/script/mogrify.php",
            "image cropping"
        );

        var imageEditToolSettings = new ExternalUtilties.UtilitySettings(
            imageProgram,
            "-trim -fuzz 10% *.jpg",
            workingDirectory
        );

        ExternalUtilties.Runner.Run(imageEditToolSettings, printer);
    }
}
