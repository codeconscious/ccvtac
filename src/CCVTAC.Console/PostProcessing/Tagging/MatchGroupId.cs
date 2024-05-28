namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// Represents the index of a specific regular expression match group.
/// </summary>
internal enum MatchGroupId : byte
{
    /// <summary>
    /// Represents the value of the entire match (and not a specified group within it).
    /// </summary>
    Zero = 0,

    /// <summary>
    /// Represents the first group of a regular expression match.
    /// </summary>
    First = 1,

    /// <summary>
    /// Represents the second group of a regular expression match.
    /// </summary>
    Second = 2,

    /// <summary>
    /// Represents the third group of a regular expression match.
    /// </summary>
    Third = 3,

    /// <summary>
    /// Represents the fourth group of a regular expression match.
    /// </summary>
    Fourth = 4,
}
