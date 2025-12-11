module ExtensionsTests

open CCVTAC.Console
open Xunit
open System

module NumericsTests =

    [<Fact>]
    let ``isZero returns true for any zero value`` () =
        Assert.True <| Numerics.isZero 0
        Assert.True <| Numerics.isZero 0u
        Assert.True <| Numerics.isZero 0us
        Assert.True <| Numerics.isZero 0.
        Assert.True <| Numerics.isZero 0L
        Assert.True <| Numerics.isZero 0m
        Assert.True <| Numerics.isZero -0
        Assert.True <| Numerics.isZero -0.
        Assert.True <| Numerics.isZero -0L
        Assert.True <| Numerics.isZero -0m

    [<Fact>]
    let ``isZero returns false for any non-zero value`` () =
        Assert.False <| Numerics.isZero 1
        Assert.False <| Numerics.isOne -1
        Assert.False <| Numerics.isOne Int64.MinValue
        Assert.False <| Numerics.isOne Int64.MaxValue
        Assert.False <| Numerics.isOne 2
        Assert.False <| Numerics.isZero 1u
        Assert.False <| Numerics.isZero 1us
        Assert.False <| Numerics.isZero -0.0000000000001
        Assert.False <| Numerics.isZero 0.0000000000001
        Assert.False <| Numerics.isZero 1.
        Assert.False <| Numerics.isZero 1L
        Assert.False <| Numerics.isZero 1m

    [<Fact>]
    let ``isOne returns true for any one value`` () =
        Assert.True <| Numerics.isOne 1
        Assert.True <| Numerics.isOne 1u
        Assert.True <| Numerics.isOne 1us
        Assert.True <| Numerics.isOne 1.
        Assert.True <| Numerics.isOne 1L
        Assert.True <| Numerics.isOne 1m

    [<Fact>]
    let ``isOne returns false for any non-one value`` () =
        Assert.False <| Numerics.isOne 0
        Assert.False <| Numerics.isOne -1
        Assert.False <| Numerics.isOne Int64.MinValue
        Assert.False <| Numerics.isOne Int64.MaxValue
        Assert.False <| Numerics.isOne 2
        Assert.False <| Numerics.isOne 0u
        Assert.False <| Numerics.isOne 16u
        Assert.False <| Numerics.isOne 0us
        Assert.False <| Numerics.isOne -0.
        Assert.False <| Numerics.isOne 0.001
        Assert.False <| Numerics.isOne 0L
        Assert.False <| Numerics.isOne 0m

module SeqTests =

    [<Fact>]
    let ``caseInsensitiveContains returns true when exact match exists`` () =
        let input = ["Hello"; "World"; "Test"]
        Assert.True <| Seq.caseInsensitiveContains "Hello" input
        Assert.True <| Seq.caseInsensitiveContains "World" input
        Assert.True <| Seq.caseInsensitiveContains "Test" input

    [<Fact>]
    let ``caseInsensitiveContains returns true when exists but case differs`` () =
        let input = ["hello"; "WORLD"; "test"]
        Assert.True <| Seq.caseInsensitiveContains "Hello" input
        Assert.True <| Seq.caseInsensitiveContains "hello" input
        Assert.True <| Seq.caseInsensitiveContains "HELLO" input
        Assert.True <| Seq.caseInsensitiveContains "wOrLd" input
        Assert.True <| Seq.caseInsensitiveContains "tESt" input
        Assert.True <| Seq.caseInsensitiveContains "TEST" input

    [<Fact>]
    let ``caseInsensitiveContains returns false when text not in sequence`` () =
        let input = ["Hello"; "World"; "Test"]
        Assert.False <| Seq.caseInsensitiveContains "Missing" input

    [<Fact>]
    let ``caseInsensitiveContains works with empty sequence`` () =
        Assert.False <| Seq.caseInsensitiveContains "Any" []

    [<Fact>]
    let ``caseInsensitiveContains handles null or empty strings`` () =
        let input = [String.Empty; null; "Test"]
        Assert.True <| Seq.caseInsensitiveContains String.Empty input
        Assert.True <| Seq.caseInsensitiveContains null input

    [<Fact>]
    let ``caseInsensitiveContains handles Japanese strings`` () =
        let input = ["関数型プログラミング"; "楽しいぞ"]
        Assert.True <| Seq.caseInsensitiveContains "関数型プログラミング" input
        Assert.False <| Seq.caseInsensitiveContains "いや、楽しくないや" input

module ListTests =

    [<Fact>]
    let ``caseInsensitiveContains returns true when exact match exists`` () =
        let input = ["Hello"; "World"; "Test"]
        Assert.True <| List.caseInsensitiveContains "Hello" input
        Assert.True <| List.caseInsensitiveContains "World" input
        Assert.True <| List.caseInsensitiveContains "Test" input

    [<Fact>]
    let ``caseInsensitiveContains returns true when exists but case differs`` () =
        let input = ["hello"; "WORLD"; "test"]
        Assert.True <| List.caseInsensitiveContains "Hello" input
        Assert.True <| List.caseInsensitiveContains "hello" input
        Assert.True <| List.caseInsensitiveContains "HELLO" input
        Assert.True <| List.caseInsensitiveContains "wOrLd" input
        Assert.True <| List.caseInsensitiveContains "tESt" input
        Assert.True <| List.caseInsensitiveContains "TEST" input

    [<Fact>]
    let ``caseInsensitiveContains returns false when text not in sequence`` () =
        let input = ["Hello"; "World"; "Test"]
        Assert.False <| List.caseInsensitiveContains "Missing" input

    [<Fact>]
    let ``caseInsensitiveContains works with empty sequence`` () =
        Assert.False <| List.caseInsensitiveContains "Any" []

    [<Fact>]
    let ``caseInsensitiveContains handles null or empty strings`` () =
        let input = [String.Empty; null; "Test"]
        Assert.True <| List.caseInsensitiveContains String.Empty input
        Assert.True <| List.caseInsensitiveContains null input

    [<Fact>]
    let ``caseInsensitiveContains handles Japanese strings`` () =
        let input = ["関数型プログラミング"; "楽しいぞ"]
        Assert.True  <| List.caseInsensitiveContains "関数型プログラミング" input
        Assert.False <| List.caseInsensitiveContains "いや、楽しくないや"   input

module ArrayTests =

    module HasMultiple =

        [<Fact>]
        let ``hasMultiple returns true for array with more than one element`` () =
            Assert.True <| Array.hasMultiple [| 1; 2; 3 |]

        [<Fact>]
        let ``hasMultiple returns false for empty array`` () =
            Assert.False <| Array.hasMultiple [||]

        [<Fact>]
        let ``hasMultiple returns false for single-element array`` () =
            Assert.False <| Array.hasMultiple [| 0 |]

        [<Fact>]
        let ``hasMultiple works with different types of arrays`` () =
            Assert.True <| Array.hasMultiple [| "hello"; "world" |]
            Assert.True <| Array.hasMultiple [| 1.0; 2.0; 3.0 |]
            Assert.True <| Array.hasMultiple [| false; true; true |]
            Assert.True <| Array.hasMultiple [| Array.sum; Array.length |]

        [<Fact>]
        let ``hasMultiple handles large arrays`` () =
            Assert.True <| Array.hasMultiple (Array.init 100 id)
