namespace CCVTAC.Console.PostProcessing

open CCVTAC.Console.ExternalTools
open CCVTAC.Console
open CCVTAC.Console.ExternalTools

module ImageProcessor =

    let internal ProgramName = "mogrify"

    let internal Run (workingDirectory: string) (printer: Printer) : unit =
        let imageEditToolSettings = {
            CommandWithArgs = $"{ProgramName} -trim -fuzz 10%% *.jpg"
            WorkingDirectory = workingDirectory
        }
        Runner.run imageEditToolSettings [||] printer |> ignore
