namespace CCVTAC.Console.PostProcessing

open CCVTAC.Console.ExternalTools

module ImageProcessor =

    let private programName = "mogrify"

    let run workingDirectory printer : unit =
        let toolSettings =
            ToolSettings.create
                $"{programName} -trim -fuzz 10%% *.jpg"
                workingDirectory

        Runner.runTool toolSettings [] printer |> ignore
