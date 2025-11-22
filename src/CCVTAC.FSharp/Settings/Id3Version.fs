namespace CCVTAC.Console.Settings

open System

module TagFormat =

    /// Point versions of ID3 version 2 (such as 2.3 or 2.4).
    type Id3V2Version =
        | TwoPoint2 = 2
        | TwoPoint3 = 3
        | TwoPoint4 = 4

    /// Locks the ID3v2.x version to a valid one and optionally forces that version.
    let SetId3V2Version (version: Id3V2Version) (forceAsDefault: bool) : unit =
        TagLib.Id3v2.Tag.DefaultVersion <- byte version
        TagLib.Id3v2.Tag.ForceDefaultVersion <- forceAsDefault
