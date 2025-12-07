module UtilitiesTests

open CCVTAC.Console
open Xunit
open System

module NumberUtilitiesTests =

    [<Fact>]
    let ``isZero returns true for any zero value`` () =
        Assert.True <| NumberUtilities.isZero 0
        Assert.True <| NumberUtilities.isZero 0u
        Assert.True <| NumberUtilities.isZero 0us
        Assert.True <| NumberUtilities.isZero 0.
        Assert.True <| NumberUtilities.isZero 0L
        Assert.True <| NumberUtilities.isZero 0m
        Assert.True <| NumberUtilities.isZero -0
        Assert.True <| NumberUtilities.isZero -0.
        Assert.True <| NumberUtilities.isZero -0L
        Assert.True <| NumberUtilities.isZero -0m

    [<Fact>]
    let ``isZero returns false for any non-zero value`` () =
        Assert.False <| NumberUtilities.isZero 1
        Assert.False <| NumberUtilities.isOne -1
        Assert.False <| NumberUtilities.isOne Int64.MinValue
        Assert.False <| NumberUtilities.isOne Int64.MaxValue
        Assert.False <| NumberUtilities.isOne 2
        Assert.False <| NumberUtilities.isZero 1u
        Assert.False <| NumberUtilities.isZero 1us
        Assert.False <| NumberUtilities.isZero 0.0000000000001
        Assert.False <| NumberUtilities.isZero 1.
        Assert.False <| NumberUtilities.isZero 1L
        Assert.False <| NumberUtilities.isZero 1m

    [<Fact>]
    let ``isOne returns true for any one value`` () =
        Assert.True <| NumberUtilities.isOne 1
        Assert.True <| NumberUtilities.isOne 1u
        Assert.True <| NumberUtilities.isOne 1us
        Assert.True <| NumberUtilities.isOne 1.
        Assert.True <| NumberUtilities.isOne 1L
        Assert.True <| NumberUtilities.isOne 1m

    [<Fact>]
    let ``isOne returns false for any non-one value`` () =
        Assert.False <| NumberUtilities.isOne 0
        Assert.False <| NumberUtilities.isOne -1
        Assert.False <| NumberUtilities.isOne Int64.MinValue
        Assert.False <| NumberUtilities.isOne Int64.MaxValue
        Assert.False <| NumberUtilities.isOne 2
        Assert.False <| NumberUtilities.isOne 0u
        Assert.False <| NumberUtilities.isOne 16u
        Assert.False <| NumberUtilities.isOne 0us
        Assert.False <| NumberUtilities.isOne -0.
        Assert.False <| NumberUtilities.isOne 0.001
        Assert.False <| NumberUtilities.isOne 0L
        Assert.False <| NumberUtilities.isOne 0m

module SeqTests =

    [<Fact>]
    let ``caseInsensitiveContains returns true when exact match exists`` () =
        let input = ["Hello"; "World"; "Test"]
        Seq.caseInsensitiveContains "Hello" input |> Assert.True
        Seq.caseInsensitiveContains "World" input |> Assert.True
        Seq.caseInsensitiveContains "Test" input  |> Assert.True

    [<Fact>]
    let ``caseInsensitiveContains returns true when exists but case differs`` () =
        let input = ["hello"; "WORLD"; "test"]
        Seq.caseInsensitiveContains "Hello" input |> Assert.True
        Seq.caseInsensitiveContains "hello" input |> Assert.True
        Seq.caseInsensitiveContains "HELLO" input |> Assert.True
        Seq.caseInsensitiveContains "wOrLd" input |> Assert.True
        Seq.caseInsensitiveContains "tESt" input  |> Assert.True
        Seq.caseInsensitiveContains "TEST" input  |> Assert.True

    [<Fact>]
    let ``caseInsensitiveContains returns false when text not in sequence`` () =
        let input = ["Hello"; "World"; "Test"]
        Seq.caseInsensitiveContains "Missing" input |> Assert.False

    [<Fact>]
    let ``caseInsensitiveContains works with empty sequence`` () =
        Seq.caseInsensitiveContains "Any" [] |> Assert.False

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

module ListTests =

    [<Fact>]
    let ``caseInsensitiveContains returns true when exact match exists`` () =
        let input = ["Hello"; "World"; "Test"]
        List.caseInsensitiveContains "Hello" input |> Assert.True
        List.caseInsensitiveContains "World" input |> Assert.True
        List.caseInsensitiveContains "Test" input |> Assert.True

    [<Fact>]
    let ``caseInsensitiveContains returns true when exists but case differs`` () =
        let input = ["hello"; "WORLD"; "test"]
        List.caseInsensitiveContains "Hello" input |> Assert.True
        List.caseInsensitiveContains "hello" input |> Assert.True
        List.caseInsensitiveContains "HELLO" input |> Assert.True
        List.caseInsensitiveContains "wOrLd" input |> Assert.True
        List.caseInsensitiveContains "tESt" input |> Assert.True
        List.caseInsensitiveContains "TEST" input |> Assert.True

    [<Fact>]
    let ``caseInsensitiveContains returns false when text not in sequence`` () =
        let input = ["Hello"; "World"; "Test"]
        List.caseInsensitiveContains "Missing" input |> Assert.False

    [<Fact>]
    let ``caseInsensitiveContains works with empty sequence`` () =
        List.caseInsensitiveContains "Any" [] |> Assert.False

    [<Fact>]
    let ``caseInsensitiveContains handles null or empty strings`` () =
        let input = [String.Empty; null; "Test"]
        List.caseInsensitiveContains String.Empty input |> Assert.True
        List.caseInsensitiveContains null input |> Assert.True

    [<Fact>]
    let ``caseInsensitiveContains handles Japanese strings`` () =
        let input = ["関数型プログラミング"; "楽しいぞ"]
        List.caseInsensitiveContains "関数型プログラミング" input |> Assert.True
        List.caseInsensitiveContains "いや、楽しくないや"   input |> Assert.False

module ArrayTests =

    module HasMultiple =

        [<Fact>]
        let ``hasMultiple returns true for array with more than one element`` () =
            Array.hasMultiple [| 1; 2; 3 |] |> Assert.True

        [<Fact>]
        let ``hasMultiple returns false for empty array`` () =
            Array.hasMultiple [||] |> Assert.False

        [<Fact>]
        let ``hasMultiple returns false for single-element array`` () =
            Array.hasMultiple [| 0 |] |> Assert.False

        [<Fact>]
        let ``hasMultiple works with different types of arrays`` () =
            Array.hasMultiple [| "hello"; "world" |]        |> Assert.True
            Array.hasMultiple [| 1.0; 2.0; 3.0 |]           |> Assert.True
            Array.hasMultiple [| false; true; true |]       |> Assert.True
            Array.hasMultiple [| Array.sum; Array.length |] |> Assert.True

        [<Fact>]
        let ``hasMultiple handles large arrays`` () =
            Array.init 100 id |> Array.hasMultiple |> Assert.True
