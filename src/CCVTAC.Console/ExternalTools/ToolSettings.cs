namespace CCVTAC.Console.ExternalTools;

/// <summary>
/// Settings to govern the behavior of an external program.
/// </summary>
internal sealed record ToolSettings(string CommandWithArgs, string WorkingDirectory);
