namespace CCVTAC.Main.IoUtilities

open CCFSharpUtils
open System.IO

module Files =

    /// Extensions of the supported audio file types.
    let audioFileExts =
        [".aac"; ".alac"; ".flac"; ".m4a"; ".mp3"; ".ogg"; ".vorbis"; ".opus"; ".wav"]

    let jsonFileExt = ".json"

    let imageFileExts = [".jpg"; ".jpeg"]

    let filterByExt ext fs = fs |> List.filter (String.endsWithIgnoreCase ext)

    let readAllText (filePath: string) : Result<string, string> =
        ofTry (fun _ -> File.ReadAllText filePath)

    let appendToFile (file: FileInfo) (text: string) : Result<unit, string> =
        ofTry (fun _ -> File.AppendAllText(file.FullName, text))
