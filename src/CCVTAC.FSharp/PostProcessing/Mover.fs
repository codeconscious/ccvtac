namespace CCVTAC.Console.PostProcessing

open System
open System.IO
open System.Linq
open System.Text.Json
open System.Text.RegularExpressions
open System.Collections.Generic
open System.Collections.Immutable
open CCVTAC.Console.PostProcessing.Tagging
open CCVTAC.FSharp.Settings

module Mover =

    let private PlaylistImageRegex = Regex(@"

\[[OP]L[\w\d_-]{12,}\]

", RegexOptions.Compiled)
    let private ImageFileWildcard = "*.jp*"

    let Run
        (taggingSets: seq<TaggingSet>)
        (maybeCollectionData: CollectionMetadata option)
        (settings: UserSettings)
        (overwrite: bool)
        (printer: Printer)
        : unit =
        printer.Debug "Starting move..."
        let watch = Watch() // assumes Watch type with ElapsedFriendly exists

        let workingDirInfo = DirectoryInfo(settings.WorkingDirectory)

        let firstTaggingSet =
            taggingSets
            |> Seq.tryHead
            |> Option.defaultWith (fun () -> failwith "No tagging sets provided")

        let subFolderName = GetSafeSubDirectoryName(maybeCollectionData, firstTaggingSet)
        let collectionName = maybeCollectionData |> Option.map (fun c -> c.Title) |> Option.defaultValue String.Empty
        let fullMoveToDir = Path.Combine(settings.MoveToDirectory, subFolderName, collectionName)

        match EnsureDirectoryExists(fullMoveToDir, printer) with
        | Error _ -> () // error already printed
        | Ok () ->
            let audioFileNames =
                workingDirInfo.EnumerateFiles()
                |> Seq.filter (fun f -> PostProcessor.AudioExtensions.CaseInsensitiveContains(f.Extension))
                |> Seq.toImmutableList

            if audioFileNames.IsEmpty then
                printer.Error "No audio filenames to move found."
            else
                printer.Debug (sprintf "Moving %d audio file(s) to \"%s\"..." audioFileNames.Count fullMoveToDir)

                let (successCount, failureCount) =
                    MoveAudioFiles(audioFileNames, fullMoveToDir, overwrite, printer)

                MoveImageFile(collectionName, subFolderName, workingDirInfo, fullMoveToDir, audioFileNames.Count, overwrite, printer)

                let fileLabel = if successCount = 1u then "file" else "files"
                printer.Info (sprintf "Moved %d audio %s in %s." successCount fileLabel watch.ElapsedFriendly)

                if failureCount > 0u then
                    let fileLabel' = if failureCount = 1u then "file" else "files"
                    printer.Warning (sprintf "However, %d audio %s could not be moved." failureCount fileLabel')

    let private IsPlaylistImage (fileName: string) =
        PlaylistImageRegex.IsMatch(fileName)

    let private GetCoverImage (workingDirInfo: DirectoryInfo) (audioFileCount: int) : FileInfo option =
        let images = workingDirInfo.EnumerateFiles(ImageFileWildcard) |> Seq.toArray
        if images.Length = 0 then None
        else
            let playlistImages = images |> Seq.filter (fun i -> IsPlaylistImage(i.FullName)) |> Seq.toList
            if playlistImages.Any() then Some (playlistImages.First())
            else if audioFileCount > 1 && images.Length = 1 then Some images.[0]
            else None

    let private EnsureDirectoryExists (moveToDir: string) (printer: Printer) : Result<unit, string> =
        try
            if Directory.Exists(moveToDir) then
                printer.Debug (sprintf "Found move-to directory \"%s\"." moveToDir)
                Ok ()
            else
                printer.Debug (sprintf "Creating move-to directory \"%s\" (based on playlist metadata)... " moveToDir, appendLineBreak = false)
                Directory.CreateDirectory(moveToDir) |> ignore
                printer.Debug "OK."
                Ok ()
        with ex ->
            printer.Error (sprintf "Error creating move-to directory \"%s\": %s" moveToDir ex.Message)
            Error String.Empty

    let private MoveAudioFiles (audioFiles: ImmutableList<FileInfo>) (moveToDir: string) (overwrite: bool) (printer: Printer) : uint32 * uint32 =
        let mutable successCount = 0u
        let mutable failureCount = 0u
        for file in audioFiles do
            try
                File.Move(file.FullName, Path.Combine(moveToDir, file.Name), overwrite)
                successCount <- successCount + 1u
                printer.Debug (sprintf "• Moved \"%s\"" file.Name)
            with ex ->
                failureCount <- failureCount + 1u
                printer.Error (sprintf "• Error moving file \"%s\": %s" file.Name ex.Message)
        (successCount, failureCount)

    let private MoveImageFile
        (maybeCollectionName: string)
        (subFolderName: string)
        (workingDirInfo: DirectoryInfo)
        (moveToDir: string)
        (audioFileCount: int)
        (overwrite: bool)
        (printer: Printer)
        : unit =
        try
            let baseFileName =
                if String.IsNullOrWhiteSpace maybeCollectionName then
                    subFolderName
                else
                    sprintf "%s - %s" subFolderName (maybeCollectionName.ReplaceInvalidPathChars())

            match GetCoverImage workingDirInfo audioFileCount with
            | None -> ()
            | Some image ->
                let dest = Path.Combine(moveToDir, sprintf "%s.jpg" (baseFileName.Trim()))
                image.MoveTo(dest, overwrite = overwrite)
                printer.Info "Moved image file."
        with ex ->
            printer.Warning (sprintf "Error copying the image file: %s" ex.Message)

    let private GetSafeSubDirectoryName (collectionData: CollectionMetadata option) (taggingSet: TaggingSet) : string =
        let workingName =
            match collectionData with
            | Some metadata when metadata.Uploader.HasText() && metadata.Title.HasText() -> metadata.Uploader
            | _ ->
                match GetParsedVideoJson taggingSet with
                | Ok v -> v.Uploader
                | Error _ -> String.Empty

        let safeName = workingName.ReplaceInvalidPathChars().Trim()
        let topicSuffix = " - Topic"
        if safeName.EndsWith(topicSuffix) then safeName.Replace(topicSuffix, String.Empty)
        else safeName

    let private GetParsedVideoJson (taggingSet: TaggingSet) : Result<VideoMetadata, string> =
        try
            let json = File.ReadAllText(taggingSet.JsonFilePath)
            try
                let videoData = JsonSerializer.Deserialize<VideoMetadata>(json)
                if isNull (box videoData) then Error (sprintf "Deserialized JSON was null for \"%s\"" taggingSet.JsonFilePath)
                else Ok videoData
            with :? JsonException as ex ->
                Error (sprintf "Error deserializing JSON from file \"%s\": %s" taggingSet.JsonFilePath ex.Message)
        with ex ->
            Error (sprintf "Error reading JSON file \"%s\": %s." taggingSet.JsonFilePath ex.Message)
