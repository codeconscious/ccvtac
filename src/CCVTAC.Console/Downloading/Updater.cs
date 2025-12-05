using CCVTAC.Console.ExternalTools;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.Downloading;

internal static class Updater
{
    private record Urls(string Primary, string? Supplementary);

    /// <summary>
    /// Completes the actual download process.
    /// </summary>
    /// <returns>A `Result` that, if successful, contains the name of the successfully downloaded format.</returns>
    internal static Result<string?> Run(UserSettings settings, Printer printer)
    {
        if (string.IsNullOrWhiteSpace(settings.DownloaderUpdateCommand))
        {
            printer.Info("No downloader update command provided, so will skip.");
            return Result.Ok();
        }

        var args = new ToolSettings(settings.DownloaderUpdateCommand, settings.WorkingDirectory!);

        var result = Runner.Run(args, otherSuccessExitCodes: [], printer);

        if (result.IsSuccess)
        {
            var (exitCode, warnings) = result.Value;

            if (exitCode != 0)
            {
                printer.Warning("Update completed with minor issues.");

                if (warnings.HasText())
                {
                    printer.Warning(warnings);
                }
            }

            return Result.Ok();
        }

        printer.Error($"Failure updating...");

        var errors = result.Errors.Select(e => e.Message).ToList();

        if (errors.Count != 0)
        {
            result.Errors.ToList().ForEach(e => printer.Error(e.Message));
        }

        return errors.Count > 0
            ? Result.Fail(string.Join(" / ", errors))
            : Result.Ok();
    }
}
