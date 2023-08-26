namespace CCVTAC.Console.ExternalUtilties;

public sealed record ExternalToolSettings(
    string Summary,
    string ProgramName,
    string Args,
    string WorkingDirectory,
    Printer Printer);
