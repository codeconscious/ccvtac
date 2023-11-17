namespace CCVTAC.Console.ExternalUtilities;

/// <summary>
/// Settings to govern the behavior of an external program.
/// </summary>
/// <param name="Program">The external utility to be executed.</param>
/// <param name="Args">All arguments to be passed to the external utility.</param>
/// <param name="WorkingDirectory">The directory in which context the utility should be run.</param>
internal sealed record UtilitySettings(
    ExternalProgram Program,
    string Args,
    string WorkingDirectory
);
