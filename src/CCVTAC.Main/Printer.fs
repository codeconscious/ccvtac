namespace CCVTAC.Main

open CCFSharpUtils
open CCFSharpUtils.Text
open System
open System.Collections.Generic
open System.Linq
open Spectre.Console

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

    /// Prints the requested number of blank lines.
    let emptyLines (count: byte) : unit =
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

    /// Toggle showing or hiding debug messages.
    member this.ShowDebug(show: bool) =
        minimumLogLevel <- (if show then Level.Debug else Level.Info)

    static member private AddMarkup(message: string, colors: ColorFormat) : string =
        match colors.Foreground, colors.Background, colors.Bold with
        | None, None, false -> message
        | fg, bg, bold ->
            let boldPart = if bold then "bold " else String.Empty
            let fgPart = defaultArg fg "default"
            let bgPart = match bg with Some b -> $" on {b}" | None -> String.Empty
            let markUp = $"{boldPart}{fgPart}{bgPart}"
            $"[{markUp}]{message}[/]"

    /// This is the method that actually prints and is called by other methods.
    member private this.Print
        (
            logLevel: Level,
            message: string,
            ?appendLineBreak: bool,
            ?prependLines: byte,
            ?appendLines: byte,
            ?processMarkup: bool
        ) : unit =

        let appendLineBreak = defaultArg appendLineBreak true
        let prependLines = defaultArg prependLines 0uy
        let appendLines = defaultArg appendLines 0uy
        let processMarkup = defaultArg processMarkup true

        if int logLevel > int minimumLogLevel then
            ()
        else
            if String.hasNoText message then
                raise (ArgumentException("message", "Message cannot be empty."))

            emptyLines prependLines

            let escapedMessage = escapeText message

            if processMarkup then
                let markedUp = Printer.AddMarkup(escapedMessage, colors[logLevel])
                AnsiConsole.Markup markedUp
            else
                // AnsiConsole.Write uses format strings internally; escapedMessage already duplicates braces
                AnsiConsole.Write escapedMessage

            if appendLineBreak then AnsiConsole.WriteLine()

            emptyLines appendLines

    member this.PrintTable(table: Table) =
        AnsiConsole.Write table

    member this.Critical(message: string) =
        this.Print(Level.Critical, message)

    member this.Error(message: string, ?appendLineBreak: bool, ?prependLines: byte, ?appendLines: byte, ?processMarkup: bool) =
        this.Print(Level.Error, message, ?appendLineBreak = appendLineBreak, ?prependLines = prependLines,
                   ?appendLines = appendLines, ?processMarkup = processMarkup)

    member this.Warning(message: string) =
        this.Print(Level.Warning, message)

    member this.Info(message: string) =
        this.Print(Level.Info, message)

    member this.Debug(message: string) =
        this.Print(Level.Debug, message)

    member this.EmptyLine() = emptyLines 1uy

    member this.GetInput(prompt: string) : string =
        emptyLines 1uy
        AnsiConsole.Ask<string>($"[skyblue1]{prompt}[/]")

    static member private Ask(title: string, options: string list) : string =
        AnsiConsole.Prompt(SelectionPrompt<string>().Title(title).AddChoices(options))

    member this.AskToBool(title: string, trueAnswer: string, falseAnswer: string) : bool =
        Printer.Ask(title, [ trueAnswer; falseAnswer ]) = trueAnswer
