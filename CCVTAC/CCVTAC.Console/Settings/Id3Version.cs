namespace CCVTAC.Console.Settings;

public sealed class TagFormat
{
    /// <summary>
    /// Subversions of ID3 version 2 (such as 2.3 or 2.4).
    /// </summary>
    public enum Id3v2Version : byte
    {
        TwoPoint2 = 2,
        TwoPoint3 = 3,
        TwoPoint4 = 4,
    }

    /// <summary>
    /// Locks the ID3v2.x version to a valid one and optionally forces that version.
    /// </summary>
    /// <param name="version">The ID3 version 2 subversion to use.</param>
    /// <param name="forceAsDefault">
    ///     When true, forces the specified version when writing the file.
    ///     When false, will defer to the version within the file, if any.
    /// </param>
    public static void SetId3v2Version(Id3v2Version version, bool forceAsDefault)
    {
        TagLib.Id3v2.Tag.DefaultVersion = (byte)version;
        TagLib.Id3v2.Tag.ForceDefaultVersion = forceAsDefault;
    }
}
