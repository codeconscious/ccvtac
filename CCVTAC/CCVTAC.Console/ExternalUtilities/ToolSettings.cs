namespace CCVTAC.Console.ExternalUtilties;

/// <summary>
/// Settings to govern the behavior of an external program.
/// </summary>
/// <param name="Description">A brief summary of what the program will do, phrased as a noun.</param>
/// <param name="ProgramName"></param>
/// <param name="Args"></param>
/// <param name="WorkingDirectory"></param>
/// <param name="Printer"></param>
public sealed record ToolSettings(
    string Description,
    string ProgramName,
    string Args,
    string WorkingDirectory,
    Printer Printer
);
