namespace CCVTAC.Main.IoUtilities

open CCFSharpUtils.Text

module Files =

    /// Extensions of the supported audio file types.
    let audioFileExts =
        [".aac"; ".alac"; ".flac"; ".m4a"; ".mp3"; ".ogg"; ".vorbis"; ".opus"; ".wav"]

    // Source: https://github.com/yt-dlp/yt-dlp/?tab=readme-ov-file#post-processing-options
    let supportedAudioFormats = [ "best"; "aac"; "alac"; "flac"; "m4a"; "mp3"; "opus"; "vorbis"; "wav" ]

    let isValidAudioFmt fmt = supportedAudioFormats |> List.contains fmt

    let jsonFileExt = ".json"

    let imageFileExts = [".jpg"; ".jpeg"]

    let supportedNormalizationForms = [ "C"; "D"; "KC"; "KD" ]

    let countFiles : int -> string =
        String.plur' "file" S

    let countFiles' description : int -> string =
        String.plur' $"{description} file" S

    let filterByExt ext files = files |> List.filter (String.endsWith' ext)

