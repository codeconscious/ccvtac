namespace CCVTAC.Console.PostProcessing

open CCVTAC.Console.IoUtilities
open CCVTAC.Console.PostProcessing.Tagging
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console
open CCVTAC.Console.PostProcessing
open System
open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Startwatch.Library
open TaggingSets

module Mover =

    let private playlistImageRegex = Regex(@"\[[OP]L[\w\d_-]{12,}\]", RegexOptions.Compiled)
    let private imageFileWildcard = "*.jp*"

    let private isPlaylistImage (fileName: string) =
        playlistImageRegex.IsMatch fileName

    let private getCoverImage (workingDirInfo: DirectoryInfo) audioFileCount : FileInfo option =
        let images = workingDirInfo.EnumerateFiles imageFileWildcard |> Seq.toList
        if List.isEmpty images then
            None
        else
            let playlistImages = images |> List.filter (fun i -> isPlaylistImage i.FullName)
            if not (List.isEmpty playlistImages)
            then Some playlistImages[0]
            elif audioFileCount > 1 && images.Length = 1
            then Some images[0]
            else None

    let private moveAudioFiles
        (audioFiles: FileInfo list)
        (moveToDir: string)
        (overwrite: bool)
        : {| Successes: string list; Failures: string list |} =

        let successes, failures = ResizeArray<string>(), ResizeArray<string>()

        for file in audioFiles do
            try
                File.Move(file.FullName, Path.Combine(moveToDir, file.Name), overwrite)
                successes.Add $"• Moved \"%s{file.Name}\""
            with exn ->
                failures.Add $"• Error moving \"%s{file.Name}\": %s{exn.Message}"

        {| Successes = successes |> Seq.toList |> List.rev
           Failures  = failures  |> Seq.toList |> List.rev |}

    let private moveImageFile
        (maybeCollectionName: string)
        (subFolderName: string)
        (workingDirInfo: DirectoryInfo)
        (moveToDir: string)
        (audioFileCount: int)
        (overwrite: bool)
        : Result<string, string> =

        try
            match getCoverImage workingDirInfo audioFileCount with
            | None ->
                Ok "No image to move was found."
            | Some fileInfo ->
                let baseFileName =
                    if String.hasNoText maybeCollectionName
                    then subFolderName
                    else $"%s{subFolderName} - %s{String.replaceInvalidPathChars None None maybeCollectionName}"
                let dest = Path.Combine(moveToDir, $"%s{baseFileName.Trim()}.jpg")
                fileInfo.MoveTo(dest, overwrite = overwrite)
                Ok $"Image file \"{fileInfo.Name}\" was moved."
        with exn ->
            Error $"Error copying the image file: %s{exn.Message}"

    let private getParsedVideoJson (taggingSet: TaggingSet) : Result<VideoMetadata, string> =
        try
            let json = File.ReadAllText taggingSet.JsonFilePath
            match JsonSerializer.Deserialize<VideoMetadata> json with
            | Null -> Error $"Deserialized JSON was null for \"%s{taggingSet.JsonFilePath}\""
            | NonNull v -> Ok v
        with
        | :? JsonException as exn ->
            Error $"Error deserializing JSON from file \"%s{taggingSet.JsonFilePath}\": %s{exn.Message}"
        | exn ->
            Error $"Error reading JSON file \"%s{taggingSet.JsonFilePath}\": %s{exn.Message}."

    let private getSafeSubDirectoryName (collectionData: CollectionMetadata option) taggingSet : string =
        let workingName =
            match collectionData with
            | Some metadata when String.hasText metadata.Uploader &&
                                 String.hasText metadata.Title -> metadata.Uploader
            | _ ->
                match getParsedVideoJson taggingSet with
                | Ok v -> v.Uploader
                | Error _ -> "COLLECTION_DATA_NOT_FOUND"

        let safeName = workingName |> String.replaceInvalidPathChars None None |> _.Trim()
        let topicSuffix = " - Topic"
        safeName.Replace(topicSuffix, String.Empty)

    let run
        (taggingSets: TaggingSet seq)
        (maybeCollectionData: CollectionMetadata option)
        (settings: UserSettings)
        (overwrite: bool)
        (printer: Printer)
        : unit =

        printer.Debug "Starting move..."
        let watch = Watch()

        let workingDirInfo = DirectoryInfo settings.WorkingDirectory

        let firstTaggingSet =
            taggingSets
            |> Seq.tryHead
            |> Option.defaultWith (fun () -> failwith "No tagging sets provided") // TODO: Improve.

        let subFolderName = getSafeSubDirectoryName maybeCollectionData firstTaggingSet
        let collectionName = maybeCollectionData |> Option.map _.Title |> Option.defaultValue String.Empty
        let fullMoveToDir = Path.Combine(settings.MoveToDirectory, subFolderName, collectionName)

        match Directories.ensureDirectoryExists fullMoveToDir with
        | Error err ->
            printer.Error err
        | Ok dirInfo ->
            printer.Debug $"Move-to directory \"%s{dirInfo.Name}\" exists."

            let audioFileNames =
                workingDirInfo.EnumerateFiles()
                |> Seq.filter (fun f -> List.caseInsensitiveContains f.Extension Files.audioFileExtensions)
                |> List.ofSeq

            if audioFileNames.IsEmpty then
                printer.Error "No audio filenames to move were found."
            else
                let fileCountMsg = String.fileLabelWithDescriptor "audio"

                printer.Debug $"Moving %s{fileCountMsg audioFileNames.Length} to \"%s{fullMoveToDir}\"..."

                let results = moveAudioFiles audioFileNames fullMoveToDir overwrite

                printer.Info $"Moved %s{fileCountMsg results.Successes.Length} in %s{watch.ElapsedFriendly}."
                results.Successes |> List.iter printer.Debug

                if List.isNotEmpty results.Failures then
                    printer.Warning $"However, %s{fileCountMsg results.Failures.Length} could not be moved:"
                    results.Failures |> List.iter printer.Error

                moveImageFile collectionName subFolderName workingDirInfo fullMoveToDir
                              audioFileNames.Length overwrite
                |> function
                | Ok msg -> printer.Info msg
                | Error err -> printer.Error $"Error moving the image file: %s{err}."
