namespace CCVTAC.Console.ExternalTools

/// Settings to govern the behavior of an external program.
type ToolSettings = {
    /// The full command with its arguments
    CommandWithArgs: string

    /// The working directory for the tool's execution
    WorkingDirectory: string
}

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ToolSettings =
    /// Creates a new ToolSettings instance
    let create (commandWithArgs: string) (workingDirectory: string) =
        {
            CommandWithArgs = commandWithArgs
            WorkingDirectory = workingDirectory
        }
