namespace CCVTAC.Console.ExternalUtilties;

public sealed record ExternalToolSettings(
    string Description,
    string ProgramName,
    string Args,
    string WorkingDirectory,
    Printer Printer
);
