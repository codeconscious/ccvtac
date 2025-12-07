module UtilitiesTests

open CCVTAC.Console
open Xunit
open System

module SeqTests =

    [<Fact>]
    let ``caseInsensitiveContains returns true when exact match exists`` () =
        let input = ["Hello"; "World"; "Test"]
        Seq.caseInsensitiveContains "Hello" input |> Assert.True
        Seq.caseInsensitiveContains "World" input |> Assert.True
        Seq.caseInsensitiveContains "Test" input |> Assert.True

    [<Fact>]
    let ``caseInsensitiveContains returns true when exists but case differs`` () =
        let input = ["hello"; "WORLD"; "test"]
        Seq.caseInsensitiveContains "Hello" input |> Assert.True
        Seq.caseInsensitiveContains "hello" input |> Assert.True
        Seq.caseInsensitiveContains "HELLO" input |> Assert.True
        Seq.caseInsensitiveContains "wOrLd" input |> Assert.True
        Seq.caseInsensitiveContains "tESt" input |> Assert.True
        Seq.caseInsensitiveContains "TEST" input |> Assert.True

    [<Fact>]
    let ``caseInsensitiveContains returns false when text not in sequence`` () =
        let input = ["Hello"; "World"; "Test"]
        Seq.caseInsensitiveContains "Missing" input |> Assert.False

    [<Fact>]
    let ``caseInsensitiveContains works with empty sequence`` () =
        let input = []
        Seq.caseInsensitiveContains "Any" input |> Assert.False

    [<Fact>]
    let ``caseInsensitiveContains handles null or empty strings`` () =
        let input = [String.Empty; null; "Test"]
        Seq.caseInsensitiveContains String.Empty input |> Assert.True
        Seq.caseInsensitiveContains null input |> Assert.True

    [<Fact>]
    let ``caseInsensitiveContains handles Japanese strings`` () =
        let input = ["関数型プログラミング"; "楽しいぞ"]
        Seq.caseInsensitiveContains "関数型プログラミング" input |> Assert.True
        Seq.caseInsensitiveContains "いや、楽しくないや" input |> Assert.False
