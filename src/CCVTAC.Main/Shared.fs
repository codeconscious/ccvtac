namespace CCVTAC.Main

open System.Threading
open Spectre.Console

[<AutoOpen>]
module Shared =

    type ResultMessageCollection = { Successes: string list; Failures: string list }

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

