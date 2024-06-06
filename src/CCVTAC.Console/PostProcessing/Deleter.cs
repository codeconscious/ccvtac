using System.IO;

namespace CCVTAC.Console.PostProcessing;

internal static class Deleter
{
    internal static void Run(
        IReadOnlyCollection<string> taggingSetFileNames,
        CollectionMetadata? collectionMetadata,
        string workingDirectory,
        bool verbose,
        Printer printer)
    {

        ImmutableList<string> collectionFileNames;
        var getFileResult = GetCollectionFiles(collectionMetadata, workingDirectory);
        if (getFileResult.IsSuccess)
        {
            var files = getFileResult.Value;
            printer.Print($"Found {files.Count} collection files.");
            collectionFileNames = files;
        }
        else
        {
            printer.Warning(getFileResult.Errors.First().Message);
            collectionFileNames = [];
        }

        var allFileNames = taggingSetFileNames.Concat(collectionFileNames).ToImmutableList();

        if (verbose)
            printer.Print($"Deleting {allFileNames.Count} temporary files...");

        foreach (var fileName in allFileNames)
        {
            try
            {
                File.Delete(fileName);

                if (verbose)
                    printer.Print($"• Deleted \"{fileName}\"");
            }
            catch (Exception ex)
            {
                printer.Error($"• Deletion error: {ex.Message}");
            }
        }

        printer.Print("Deleted temporary files.");
    }

    internal static Result<ImmutableList<string>> GetCollectionFiles(
        CollectionMetadata? collectionMetadata,
        string workingDirectory)
    {
        if (collectionMetadata is null)
            return Result.Ok(ImmutableList<string>.Empty);

        try
        {
            var id = collectionMetadata.Value.Id;
            return Directory.GetFiles(workingDirectory, $"*{id}*").ToImmutableList();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error collecting filenames: {ex.Message}");
        }
    }

    internal static void VerifyEmptyDirectory(string workingDirectory, Printer printer)
    {
        var tempFiles = IoUtilties.Directories.GetDirectoryFiles(workingDirectory);

        if (tempFiles.IsEmpty)
            return;

        printer.Warning($"{tempFiles.Count} file(s) unexpectedly remain in the working folder:");
        tempFiles.ForEach(file => printer.Warning($"• {file}"));

    }
}
