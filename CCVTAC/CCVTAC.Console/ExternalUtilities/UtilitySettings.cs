namespace CCVTAC.Console.ExternalUtilities;

/// <summary>
/// Settings to govern the behavior of an external program.
/// </summary>
/// <param name="Program">The utility to be run.</param>
/// <param name="Args">All arguments to be passed to the external utility.</param>
/// <param name="WorkingDirectory"></param>
internal sealed record UtilitySettings(
    ExternalProgram Program,
    string Args,
    string WorkingDirectory
);
