using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UserSettings = CCVTAC.FSharp.Settings.UserSettings;

namespace CCVTAC.Console.PostProcessing;

internal static class Renamer
{
    public static void Run(
        UserSettings settings,
        string workingDirectory,
        Printer printer)
    {
        Watch watch = new();

        DirectoryInfo dir = new(workingDirectory);
        var audioFilePaths = dir.EnumerateFiles("*.m4a");

        if (audioFilePaths.None())
        {
            printer.Warning("No audio files to rename were found.");
            return;
        }

        printer.Debug($"Renaming {audioFilePaths.Count()} audio file(s)...");

        string newFileName;
        Regex regex;
        Match match;
        string matchedPatternSummary;

        foreach (FileInfo filePath in audioFilePaths)
        {
            newFileName =
                settings.RenamePatterns.Aggregate(
                    new StringBuilder(filePath.Name), // Seed is the original filename
                    (newNameSb, renamePattern) =>
                    {
                        regex = new Regex(renamePattern.Regex);
                        match = regex.Match(newNameSb.ToString());

                        if (!match.Success)
                        {
                            return newNameSb; // Continue to the next iteration.
                        }

                        if (settings.VerboseOutput) // TODO: Think about this.
                        {
                            matchedPatternSummary = renamePattern.Summary is null
                                ? $"`{renamePattern.Regex}` (no description)"
                                : $"\"{renamePattern.Summary}\"";

                            printer.Debug($"Rename pattern {matchedPatternSummary} matched.");
                        }

                        // Delete the matched substring from the filename by index.
                        newNameSb.Remove(match.Index, match.Length);

                        // Generate replacement text to be inserted at the same starting index
                        // using the matches and the replacement patterns from the settings.
                        // Match #1 correllates to placeholder #1.
                        string insertText =
                            match.Groups.OfType<Group>()
                                // `Select()` indexing begins at 0, but usable matches begin at 1,
                                // so add 1 to both the match group and replacement placeholder indices.
                                .Select((gr, i) =>
                                (
                                    SearchFor:   $"%<{i + 1}>s", // Start with placeholder #1 because...
                                    ReplaceWith: match.Groups[i + 1].Value.Trim() // ...we start with regex group #1.
                                ))
                                // Starting with the placeholder text from the settings, replace each
                                // individual placeholder with the corrollated match text, then
                                // return the final string.
                                .Aggregate(
                                    new StringBuilder(renamePattern.ReplaceWithPattern),
                                    (workingText, replacementParts) =>
                                        workingText.Replace(
                                            replacementParts.SearchFor,
                                            replacementParts.ReplaceWith),
                                    workingText => workingText.ToString()
                                );

                        // Insert the final text at the same starting position.
                        newNameSb.Insert(match.Index, insertText);

                        return newNameSb;
                    },
                    newFileNameSb => newFileNameSb.ToString());

            try
            {
                File.Move(
                    filePath.FullName,
                    Path.Combine(workingDirectory, newFileName));

                printer.Debug($"• From: \"{filePath.Name}\"");
                printer.Debug($"    To: \"{newFileName}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"• Could not rename \"{filePath.Name}\": {ex.Message}");
            }
        }

        printer.Info($"Renaming done in {watch.ElapsedFriendly}.");
    }
}
