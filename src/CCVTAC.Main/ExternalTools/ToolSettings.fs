namespace CCVTAC.Main.ExternalTools

/// Settings to govern the behavior of an external program.
type ToolSettings = private {
    CommandWithArgs: string
    WorkingDirectory: string
}

module ToolSettings =
    let create commandWithArgs workingDirectory =
        { CommandWithArgs = commandWithArgs
          WorkingDirectory = workingDirectory }

