using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.PostProcessing;

internal static class Renamer
{
    public static void Run(UserSettings settings, string workingDirectory, bool isVerbose, Printer printer)
    {
        Watch watch = new();

        DirectoryInfo dir = new(workingDirectory);

        var audioFilePaths = dir.EnumerateFiles("*.m4a");
        if (!audioFilePaths.Any())
        {
            printer.Warning("No audio files to rename were found.");
            return;
        }

        if (isVerbose)
            printer.Print($"Renaming {audioFilePaths.Count()} audio file(s)...");

        string newFileName;
        Regex regex;
        Match match;
        string matchedPatternSummary;

        foreach (FileInfo filePath in audioFilePaths)
        {
            newFileName =
                settings.RenamePatterns.Aggregate(
                    new StringBuilder(filePath.Name),
                    (newFileNameSb, renamePattern) =>
                    {
                        // Only continue if the current regex is a match.
                        regex = new Regex(renamePattern.Regex);
                        match = regex.Match(newFileNameSb.ToString());

                        if (!match.Success)
                        {
                            return newFileNameSb; // Continue to the next iteration.
                        }

                        if (isVerbose)
                        {
                            matchedPatternSummary = renamePattern.Description is null
                                ? $"`{renamePattern.Regex}` (no description)"
                                : $"\"{renamePattern.Description}\"";

                            printer.Print($"Rename pattern {matchedPatternSummary} matched.");
                        }

                        // Delete the matched substring by index.
                        newFileNameSb.Remove(match.Index, match.Length);

                        // Work out the replacement text that should be inserted.
                        string insertText =
                            match.Groups.OfType<Group>()
                                .Select((gr, i) =>
                                (
                                    SearchFor:   $"%<{i + 1}>s",
                                    ReplaceWith: match.Groups[i + 1].Value
                                ))
                                .Aggregate(
                                    new StringBuilder(renamePattern.ReplaceWithPattern),
                                    (workingText, replacementParts) =>
                                        workingText.Replace(
                                            replacementParts.SearchFor,
                                            replacementParts.ReplaceWith),
                                    workingText => workingText.ToString()
                                );

                        // Insert the replacement text at the same starting position.
                        newFileNameSb.Insert(match.Index, insertText);

                        return newFileNameSb;
                    },
                    newFileNameSb => newFileNameSb.ToString());

            try
            {
                File.Move(
                    filePath.FullName,
                    Path.Combine(workingDirectory, newFileName));

                if (isVerbose)
                {
                    printer.Print($"• From: \"{filePath.Name}\"");
                    printer.Print($"    To: \"{newFileName}\"");
                }
            }
            catch (Exception ex)
            {
                printer.Error($"• Could not rename \"{filePath.Name}\": {ex.Message}");
            }
        }

        printer.Print($"Renaming done in {watch.ElapsedFriendly}.");
    }
}
