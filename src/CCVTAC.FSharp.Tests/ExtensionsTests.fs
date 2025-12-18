module ExtensionsTests

open CCVTAC.Console
open Xunit
open System

module NumericsTests =
    open CCVTAC.Console.Numerics

    [<Fact>]
    let ``isZero returns true for any zero value`` () =
        Assert.True <| isZero 0
        Assert.True <| isZero 0u
        Assert.True <| isZero 0us
        Assert.True <| isZero 0.
        Assert.True <| isZero 0L
        Assert.True <| isZero 0m
        Assert.True <| isZero -0
        Assert.True <| isZero -0.
        Assert.True <| isZero -0L
        Assert.True <| isZero -0m

    [<Fact>]
    let ``isZero returns false for any non-zero value`` () =
        Assert.False <| isZero 1
        Assert.False <| isOne -1
        Assert.False <| isOne Int64.MinValue
        Assert.False <| isOne Int64.MaxValue
        Assert.False <| isOne 2
        Assert.False <| isZero 1u
        Assert.False <| isZero 1us
        Assert.False <| isZero -0.0000000000001
        Assert.False <| isZero 0.0000000000001
        Assert.False <| isZero 1.
        Assert.False <| isZero 1L
        Assert.False <| isZero 1m

    [<Fact>]
    let ``isOne returns true for any one value`` () =
        Assert.True <| isOne 1
        Assert.True <| isOne 1u
        Assert.True <| isOne 1us
        Assert.True <| isOne 1.
        Assert.True <| isOne 1L
        Assert.True <| isOne 1m

    [<Fact>]
    let ``isOne returns false for any non-one value`` () =
        Assert.False <| isOne 0
        Assert.False <| isOne -1
        Assert.False <| isOne Int64.MinValue
        Assert.False <| isOne Int64.MaxValue
        Assert.False <| isOne 2
        Assert.False <| isOne 0u
        Assert.False <| isOne 16u
        Assert.False <| isOne 0us
        Assert.False <| isOne -0.
        Assert.False <| isOne 0.001
        Assert.False <| isOne 0L
        Assert.False <| isOne 0m

    module FormatNumberTests =

        // A tiny custom type that implements the required ToString signature.
        type MyCustomNum(i: int) =
            member _.ToString(fmt: string, provider: IFormatProvider) =
                i.ToString(fmt, provider)

        [<Fact>]
        let ``format int`` () =
            let actual = formatNumber 123456
            Assert.Equal("123,456", actual)

        [<Fact>]
        let ``format negative int`` () =
            let actual = formatNumber -1234
            Assert.Equal("-1,234", actual)

        [<Fact>]
        let ``format zero`` () =
            let actual = formatNumber 0
            Assert.Equal("0", actual)

        [<Fact>]
        let ``format int64`` () =
            let actual = formatNumber 1234567890L
            Assert.Equal("1,234,567,890", actual)

        [<Fact>]
        let ``format decimal rounds to integer display`` () =
            let actual = formatNumber 123456.78M
            Assert.Equal("123,457", actual)

        [<Fact>]
        let ``format float rounds to integer display`` () =
            let actual = formatNumber 123456.78
            Assert.Equal("123,457", actual)

        [<Fact>]
        let ``format negative float rounds to integer display`` () =
            let actual = formatNumber -1234.56
            Assert.Equal("-1,235", actual)

        [<Fact>]
        let ``format custom numeric type`` () =
            let myNum = MyCustomNum 1234
            let actual = formatNumber myNum
            Assert.Equal("1,234", actual)

module StringTests =
    open CCVTAC.Console.String

    [<Fact>]
    let ``fileLabel formats correctly`` () =
        Assert.True <| (fileLabel 0 = "0 files")
        Assert.True <| (fileLabel 1 = "1 file")
        Assert.True <| (fileLabel 2 = "2 files")
        Assert.True <| (fileLabel 1_000_000 = "1,000,000 files")

    [<Fact>]
    let ``fileLabelWithDescriptor formats correctly`` () =
        Assert.True <| (fileLabelWithDescriptor "audio" 0 = "0 audio files")
        Assert.True <| (fileLabelWithDescriptor " temporary " 1 = "1 temporary file")
        Assert.True <| (fileLabelWithDescriptor "deleted" 2 = "2 deleted files")
        Assert.True <| (fileLabelWithDescriptor "image" 1_000_000 = "1,000,000 image files")

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
