namespace CCVTAC.Console


open System.Threading
open Spectre.Console

[<AutoOpen>]
module Shared =

    let audioExtensions =
        [ ".aac"; ".alac"; ".flac"; ".m4a"; ".mp3"; ".ogg"; ".vorbis"; ".opus"; ".wav" ]

    let sleep workingMsgFn doneMsgFn seconds : string =
        let rec loop remaining (ctx: StatusContext) =
            if remaining > 0us then
                ctx.Status (workingMsgFn remaining) |> ignore
                Thread.Sleep 1000
                loop (remaining - 1us) ctx

        AnsiConsole
            .Status()
            .Start((workingMsgFn seconds), fun ctx ->
                ctx.Spinner(Spinner.Known.Star)
                   .SpinnerStyle(Style.Parse "blue")
                |> loop seconds)

        doneMsgFn seconds
