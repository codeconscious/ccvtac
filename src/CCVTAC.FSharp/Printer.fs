namespace CCVTAC.Console

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

    let extractedErrors (result: Result<'a,'b list>) : 'b list =
        match result with
        | Ok _ -> []
        | Error errors -> errors

    /// Show or hide debug messages.
    member this.ShowDebug(show: bool) =
        minimumLogLevel <- (if show then Level.Debug else Level.Info)

    /// Escape text so Spectre markup and format strings are safe.
    static member private EscapeText(text: string) : string =
        text.Replace("{", "{{")
            .Replace("}", "}}")
            .Replace("[", "[[")
            .Replace("]", "]]")

    static member private AddMarkup(message: string, colors: ColorFormat) : string =
        match colors.Foreground, colors.Background, colors.Bold with
        | None, None, false -> message
        | fg, bg, bold ->
            let boldPart = if bold then "bold " else String.Empty
            let fgPart = defaultArg fg "default"
            let bgPart = match bg with Some b -> $" on {b}" | None -> String.Empty
            let markUp = $"{boldPart}{fgPart}{bgPart}"
            $"[{markUp}]{message}[/]"

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
                raise (ArgumentNullException("message", "Message cannot be empty."))

            Printer.EmptyLines prependLines

            let escapedMessage = Printer.EscapeText message

            if processMarkup then
                let markedUp = Printer.AddMarkup(escapedMessage, colors[logLevel])
                AnsiConsole.Markup markedUp
            else
                // AnsiConsole.Write uses format strings internally; escapedMessage already duplicates braces
                AnsiConsole.Write escapedMessage

            if appendLineBreak then AnsiConsole.WriteLine()

            Printer.EmptyLines appendLines

    static member PrintTable(table: Table) =
        AnsiConsole.Write table

    member this.Critical(message: string, ?appendLineBreak: bool, ?prependLines: byte, ?appendLines: byte, ?processMarkup: bool) =
        this.Print(Level.Critical, message, ?appendLineBreak = appendLineBreak, ?prependLines = prependLines,
                   ?appendLines = appendLines, ?processMarkup = processMarkup)

    member this.Error(message: string, ?appendLineBreak: bool, ?prependLines: byte, ?appendLines: byte, ?processMarkup: bool) =
        this.Print(Level.Error, message, ?appendLineBreak = appendLineBreak, ?prependLines = prependLines,
                   ?appendLines = appendLines, ?processMarkup = processMarkup)

    member this.Errors(errors: string seq, ?appendLines: byte) =
        if Seq.isEmpty errors then raise (ArgumentException("No errors were provided!", "errors"))
        for err in errors |> Seq.filter String.hasText do
            this.Error err
        Printer.EmptyLines(defaultArg appendLines 0uy)

    member private this.Errors(headerMessage: string, errors: string seq) =
        // Create an array with headerMessage followed by the items in errors
        let items = seq { yield headerMessage; yield! errors } |> Seq.toArray
        this.Errors(items, 0uy)

    member this.Errors<'a>(failResult: Result<'a, string list>, ?appendLines: byte) =
        this.Errors(extractedErrors failResult, ?appendLines = appendLines)

    member this.Errors<'a>(headerMessage: string, failingResult: Result<'a, string list>) =
        this.Errors(headerMessage, extractedErrors failingResult)

    member this.Warning(message: string, ?appendLineBreak: bool, ?prependLines: byte, ?appendLines: byte, ?processMarkup: bool) =
        this.Print(Level.Warning, message, ?appendLineBreak = appendLineBreak, ?prependLines = prependLines,
                   ?appendLines = appendLines, ?processMarkup = processMarkup)

    member this.Info(message: string, ?appendLineBreak: bool, ?prependLines: byte, ?appendLines: byte, ?processMarkup: bool) =
        this.Print(Level.Info, message, ?appendLineBreak = appendLineBreak, ?prependLines = prependLines,
                   ?appendLines = appendLines, ?processMarkup = processMarkup)

    member this.Debug(message: string, ?appendLineBreak: bool, ?prependLines: byte, ?appendLines: byte, ?processMarkup: bool) =
        this.Print(Level.Debug, message, ?appendLineBreak = appendLineBreak, ?prependLines = prependLines,
                   ?appendLines = appendLines, ?processMarkup = processMarkup)

    /// Prints the requested number of blank lines.
    static member EmptyLines(count: byte) =
        if Numerics.isZero count
        then ()
        else
            // Write count blank lines. The original wrote (count - 1) extra NewLines inside WriteLine call.
            let repeats = int count - 1
            if repeats <= 0
            then AnsiConsole.WriteLine()
            else Enumerable.Repeat(String.newLine, repeats) |> String.Concat |> AnsiConsole.WriteLine

    member this.GetInput(prompt: string) : string =
        Printer.EmptyLines 1uy
        AnsiConsole.Ask<string>($"[skyblue1]{prompt}[/]")

    static member private Ask(title: string, options: string list) : string =
        AnsiConsole.Prompt(SelectionPrompt<string>().Title(title).AddChoices(options))

    member this.AskToBool(title: string, trueAnswer: string, falseAnswer: string) : bool =
        Printer.Ask(title, [ trueAnswer; falseAnswer ]) = trueAnswer
