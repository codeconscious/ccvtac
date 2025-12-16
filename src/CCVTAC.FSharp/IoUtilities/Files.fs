namespace CCVTAC.Console.IoUtilities

open System.IO
open CCVTAC.Console

module FileIo =

    let audioFileExtensions =
        [ ".aac"; ".alac"; ".flac"; ".m4a"; ".mp3"; ".ogg"; ".vorbis"; ".opus"; ".wav" ]

    let readAllText (filePath: string) : Result<string, string> =
        ofTry (fun _ -> File.ReadAllText filePath)

    let appendToFile (filePath: string) (text: string) : Result<unit, string> =
        ofTry (fun _ -> File.AppendAllText(filePath, text))
