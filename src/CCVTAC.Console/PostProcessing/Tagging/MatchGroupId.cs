namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// The index of a specific regular expression match group.
/// </summary>
internal enum MatchGroupId : byte
{
    /// <summary>
    /// A group representing the entirety of the matched text.
    /// </summary>
    Zero = 0,

    /// <summary>
    /// The first group of a regular expression match.
    /// </summary>
    First = 1,

    /// <summary>
    /// The second group of a regular expression match.
    /// </summary>
    Second = 2,

    /// <summary>
    /// The third group of a regular expression match.
    /// </summary>
    Third = 3,

    /// <summary>
    /// The fourth group of a regular expression match.
    /// </summary>
    Fourth = 4,

    /// <summary>
    /// The fifth group of a regular expression match.
    /// </summary>
    Fifth = 5,
}
