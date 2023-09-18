namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Video metadata fields that can contain information to be detected
/// and later assigned to audio file tags.
/// </summary>
internal enum SourceField : byte
{
    /// <summary>
    /// The video metadata's `title` field.
    /// </summary>
    Title,

    /// <summary>
    /// The video metadata's `description` field.
    /// </summary>
    Description
}
