namespace CCVTAC.Console.PostProcessing

open CCVTAC.Console.ExternalTools

module ImageProcessor =

    let internal ProgramName = "mogrify"

    let internal Run (workingDirectory: string) (printer: Printer) : unit =
        let imageEditToolSettings = ToolSettings($"{ProgramName} -trim -fuzz 10% *.jpg", workingDirectory)
        Runner.Run(imageEditToolSettings, [||], printer) |> ignore
