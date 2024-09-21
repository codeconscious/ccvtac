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

        var workingDirInfo = new DirectoryInfo(workingDirectory);
        var audioFiles = workingDirInfo
            .EnumerateFiles()
            .Where(f => PostProcessor.AudioExtensions.CaseInsensitiveContains(f.Extension))
            .ToImmutableList();

        if (audioFiles.None())
        {
            printer.Warning("No audio files to rename were found.");
            return;
        }

        printer.Debug($"Renaming {audioFiles.Count} audio file(s)...");

        string newFileName;
        Regex regex;
        MatchCollection allMatches;
        List<Match> successMatches;
        string matchedPatternSummary;

        foreach (FileInfo file in audioFiles)
        {
            newFileName =
                settings.RenamePatterns.Aggregate(
                    new StringBuilder(file.Name), // Seed
                    (newNameSb, renamePattern) =>
                    {
                        regex = new Regex(renamePattern.RegexPattern);

                        // There will be multiple matches if the multiple instances are found.
                        allMatches = regex.Matches(newNameSb.ToString());

                        // Reverse to ensure processing starts at the end of the string
                        // (to avoid indexing errors).
                        successMatches = allMatches.Where(m => m.Success).Reverse().ToList();

                        if (successMatches.Count == 0)
                        {
                            return newNameSb;
                        }

                        if (!settings.QuietMode)
                        {
                            matchedPatternSummary = renamePattern.Summary is null
                                ? $"`{renamePattern.RegexPattern}` (no description)"
                                : $"\"{renamePattern.Summary}\"";

                            printer.Debug($"Rename pattern {matchedPatternSummary} matched {successMatches.Count} time(s).");
                        }

                        foreach (Match match in successMatches)
                        {
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
                                        new StringBuilder(renamePattern.ReplaceWithPattern), // Seed
                                        (workingText, replacementParts) =>
                                            workingText.Replace(
                                                replacementParts.SearchFor,
                                                replacementParts.ReplaceWith),
                                        workingText => workingText.ToString()
                                    );

                            // Insert the final text at the same starting position.
                            newNameSb.Insert(match.Index, insertText);
                        }

                        return newNameSb;
                    },
                    newFileNameSb => newFileNameSb.ToString());

            try
            {
                File.Move(
                    file.FullName,
                    Path.Combine(workingDirectory, newFileName));

                printer.Debug($"• From: \"{file.Name}\"");
                printer.Debug($"    To: \"{newFileName}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"• Error renaming \"{file.Name}\": {ex.Message}");
            }
        }

        printer.Info($"Renaming done in {watch.ElapsedFriendly}.");
    }
}
