namespace CCVTAC.Main

open CCFSharpUtils
open CCFSharpUtils.Text
open System
open System.Collections.Generic
open System.Linq
open Spectre.Console
open FSharpPlus

type private Level =
    | Critical = 0
    | Error = 1
    | Warning = 2
    | Info = 3
    | Debug = 4

type private ColorFormat =
    { Foreground: string option
      Background: string option
      Bold: bool }

type PrintOptions =
    { AppendLineBreak: bool
      PrependLines: byte
      AppendLines: byte
      ProcessMarkup: bool }

    static member Default =
        { AppendLineBreak = true
          PrependLines = 0uy
          AppendLines = 0uy
          ProcessMarkup = true }

type Printer(showDebug: bool) =

    static let colors : Dictionary<Level, ColorFormat> =
        Dictionary()
        |> fun d ->
            d.Add(Level.Critical, { Foreground = Some "white";  Background = Some "red3"; Bold = true  })
            d.Add(Level.Error,    { Foreground = Some "red";    Background = None;        Bold = false })
            d.Add(Level.Warning,  { Foreground = Some "yellow"; Background = None;        Bold = false })
            d.Add(Level.Info,     { Foreground = None;          Background = None;        Bold = false })
            d.Add(Level.Debug,    { Foreground = Some "grey70"; Background = None;        Bold = false })
            d

    let mutable minimumLogLevel =
        if showDebug then Level.Debug else Level.Info

    let printEmptyLines count : unit =
        if Num.isPos count
        then
            let repeats = int count - 1
            if repeats < 1
            then AnsiConsole.WriteLine()
            else Enumerable.Repeat(String.nl, repeats)
                 |> String.Concat
                 |> AnsiConsole.WriteLine

    let extractedErrors (result: Result<'a,'b list>) : 'b list =
        match result with
        | Ok _ -> []
        | Error errors -> errors

    /// Escape text so Spectre markup and format strings are safe.
    let escapeText (text: string) : string =
        text.Replace("{", "{{")
            .Replace("}", "}}")
            .Replace("[", "[[")
            .Replace("]", "]]")

    let addMarkup message colorFormat : string =
        match colorFormat.Foreground, colorFormat.Background, colorFormat.Bold with
        | None, None, false -> message
        | fg, bg, bold ->
            let boldPart = if bold then "bold " else String.Empty
            let fgPart = defaultArg fg "default"
            let bgPart = match bg with Some b -> $" on {b}" | None -> String.Empty
            let markUp = $"{boldPart}{fgPart}{bgPart}"
            $"[{markUp}]{message}[/]"

    let ask title (choices: string list) : string =
        AnsiConsole.Prompt(SelectionPrompt<string>().Title(title).AddChoices(choices))

    let print logLevel message maybePrintOptions : unit =
        if logLevel > minimumLogLevel then
            ()
        else
            let printOptions = maybePrintOptions |> option id PrintOptions.Default

            if String.hasNoText message then
                raise (ArgumentException(nameof(message), "Message cannot be empty."))

            printEmptyLines printOptions.PrependLines

            let escapedMessage = escapeText message

            if printOptions.ProcessMarkup then
                let markedUp = addMarkup escapedMessage colors[logLevel]
                AnsiConsole.Markup markedUp
            else
                // AnsiConsole.Write uses format strings internally; escapedMessage already duplicates braces.
                AnsiConsole.Write escapedMessage

            if printOptions.AppendLineBreak then
                AnsiConsole.WriteLine()

            printEmptyLines printOptions.AppendLines

    /// Toggle showing or hiding debug messages.
    member this.ShowDebug(shouldEnable) =
        minimumLogLevel <- (if shouldEnable then Level.Debug else Level.Info)

    member this.PrintTable(table: Table) =
        AnsiConsole.Write table

    member this.Critical(message) =
        print Level.Critical message None

    member this.Error(message) =
        print Level.Error message None

    member this.Error(message, printOptions) =
        print Level.Error message (Some printOptions)

    member this.Warning(message) =
        print Level.Warning message None

    member this.Info(message) =
        print Level.Info message None

    member this.Debug(message) =
        print Level.Debug message None

    member this.EmptyLine() = printEmptyLines 1uy

    member this.GetInput(prompt) : string =
        printEmptyLines 1uy
        AnsiConsole.Ask<string>($"[skyblue1]{prompt}[/]")

    member this.AskToBool(question, trueAnswer, falseAnswer) : bool =
        (ask question [ trueAnswer; falseAnswer ]) = trueAnswer
