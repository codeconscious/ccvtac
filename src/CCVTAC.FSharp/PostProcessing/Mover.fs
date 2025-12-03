namespace CCVTAC.Console.PostProcessing

open CCVTAC.Console.PostProcessing.Tagging
open CCVTAC.Console.Settings.Settings
open CCVTAC.Console
open CCVTAC.Console.PostProcessing
open System
open System.IO
open System.Linq
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
        let images = workingDirInfo.EnumerateFiles imageFileWildcard |> Seq.toArray
        if images.Length = 0 then None
        else
            let playlistImages = images |> Array.filter (fun i -> isPlaylistImage i.FullName) |> Array.toList
            if playlistImages.Any() then Some (playlistImages.First())
            else if audioFileCount > 1 && images.Length = 1 then Some images[0]
            else None

    let private ensureDirectoryExists (moveToDir: string) (printer: Printer) : Result<unit, string> =
        try
            if Directory.Exists moveToDir then
                printer.Debug $"Found move-to directory \"%s{moveToDir}\"."
                Ok ()
            else
                printer.Debug ($"Creating move-to directory \"%s{moveToDir}\" (based on playlist metadata)... ", appendLineBreak = false)
                Directory.CreateDirectory moveToDir |> ignore
                printer.Debug "OK."
                Ok ()
        with ex ->
            printer.Error $"Error creating move-to directory \"%s{moveToDir}\": %s{ex.Message}"
            Error String.Empty // TODO: Update.

    let private moveAudioFiles
        (audioFiles: FileInfo list)
        (moveToDir: string)
        (overwrite: bool)
        (printer: Printer)
        : uint32 * uint32 = // TODO: Need a custom type for clarity.

        let mutable successCount = 0u
        let mutable failureCount = 0u

        for file in audioFiles do
            try
                File.Move(file.FullName, Path.Combine(moveToDir, file.Name), overwrite)
                successCount <- successCount + 1u
                printer.Debug $"• Moved \"%s{file.Name}\""
            with ex ->
                failureCount <- failureCount + 1u
                printer.Error $"• Error moving file \"%s{file.Name}\": %s{ex.Message}"

        (successCount, failureCount)

    let private moveImageFile
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
                if String.hasNoText maybeCollectionName then
                    subFolderName
                else
                    $"%s{subFolderName} - %s{String.replaceInvalidPathChars None None maybeCollectionName}"

            match getCoverImage workingDirInfo audioFileCount with
            | None -> ()
            | Some image ->
                let dest = Path.Combine(moveToDir, $"%s{baseFileName.Trim()}.jpg")
                image.MoveTo(dest, overwrite = overwrite)
                printer.Info "Moved image file."
        with ex ->
            printer.Warning $"Error copying the image file: %s{ex.Message}"

    let private getParsedVideoJson (taggingSet: TaggingSet) : Result<VideoMetadata, string> =
        try
            let json = File.ReadAllText(taggingSet.JsonFilePath)

            try
                #nowarn 3265
                let videoData = JsonSerializer.Deserialize<VideoMetadata>(json)
                #warnon 3265

                if isNull (box videoData)
                then Error $"Deserialized JSON was null for \"%s{taggingSet.JsonFilePath}\""
                else Ok videoData
            with :? JsonException as ex ->
                Error $"Error deserializing JSON from file \"%s{taggingSet.JsonFilePath}\": %s{ex.Message}"
        with ex ->
            Error $"Error reading JSON file \"%s{taggingSet.JsonFilePath}\": %s{ex.Message}."

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
        if safeName.EndsWith topicSuffix
        then safeName.Replace(topicSuffix, String.Empty)
        else safeName

    let run
        (taggingSets: seq<TaggingSet>)
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

        match ensureDirectoryExists fullMoveToDir printer with
        | Error _ -> () // Error was already printed.
        | Ok () ->
            let audioFileNames =
                workingDirInfo.EnumerateFiles()
                |> Seq.filter (fun f -> List.caseInsensitiveContains f.Extension audioExtensions)
                |> List.ofSeq

            if audioFileNames.IsEmpty then
                printer.Error "No audio filenames to move found."
            else
                printer.Debug $"Moving %d{audioFileNames.Length} audio file(s) to \"%s{fullMoveToDir}\"..."

                let successCount, failureCount =
                    moveAudioFiles audioFileNames fullMoveToDir overwrite printer

                moveImageFile collectionName subFolderName workingDirInfo fullMoveToDir audioFileNames.Length overwrite printer

                let fileLabel = if successCount = 1u then "file" else "files"
                printer.Info $"Moved %d{successCount} audio %s{fileLabel} in %s{watch.ElapsedFriendly}."

                if failureCount > 0u then
                    let fileLabel' = if failureCount = 1u then "file" else "files"
                    printer.Warning $"However, %d{failureCount} audio %s{fileLabel'} could not be moved."
