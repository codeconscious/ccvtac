namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Represents the index of a specific regular expression match group.
/// </summary>
internal enum MatchGroup : byte
{
    /// <summary>
    /// Represents the value of the entire match (and not a specified group within it).
    /// </summary>
    Group0 = 0,

    /// <summary>
    /// Represents the first group of a regular expression match.
    /// </summary>
    Group1 = 1,

    /// <summary>
    /// Represents the second group of a regular expression match.
    /// </summary>
    Group2 = 2,

    /// <summary>
    /// Represents the third group of a regular expression match.
    /// </summary>
    Group3 = 3,

    /// <summary>
    /// Represents the fourth group of a regular expression match.
    /// </summary>
    Group4 = 4,
}
