namespace CCVTAC.Main.IoUtilities

open CCFSharpUtils
open CCFSharpUtils.Text
open System.IO

module Files =

    /// Extensions of the supported audio file types.
    let audioFileExts =
        [".aac"; ".alac"; ".flac"; ".m4a"; ".mp3"; ".ogg"; ".vorbis"; ".opus"; ".wav"]

    let jsonFileExt = ".json"

    let imageFileExts = [".jpg"; ".jpeg"]

    let countFiles : int -> string =
        String.plur' "file" S

    let countFiles' description : int -> string =
        String.plur' $"{description} file" S

    let filterByExt ext files = files |> List.filter (String.endsWith' ext)

    // TODO: Use the version in CCFSharpUtils instead.
    let appendToFile (file: FileInfo) (text: string) : Result<unit, string> =
        ofTry (fun _ -> File.AppendAllText(file.FullName, text))
