namespace CCVTAC.Console.ExternalUtilties;

/// <summary>
/// Settings to govern the behavior of an external program.
/// </summary>
/// <param name="Description">A brief summary of what the program will do, phrased as a noun.</param>
/// <param name="ProgramName">The name of the utility to be run.</param>
/// <param name="Args">All arguments to be passed to the external utility.</param>
/// <param name="WorkingDirectory"></param>
public sealed record UtilitySettings(
    string Description,
    string ProgramName,
    string Args,
    string WorkingDirectory
);
