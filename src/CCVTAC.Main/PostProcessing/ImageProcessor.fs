namespace CCVTAC.Main.PostProcessing

open CCVTAC.Main.ExternalTools

module ImageProcessor =

    let private programName = "mogrify"

    let run workingDirectory printer : unit =
        let toolSettings = workingDirectory |> ToolSettings.create $"{programName} -trim -fuzz 10%% *.jpg"
        Runner.runTool toolSettings [] printer |> ignore
